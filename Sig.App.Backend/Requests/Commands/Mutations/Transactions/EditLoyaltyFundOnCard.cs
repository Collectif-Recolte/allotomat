using System.Collections.Generic;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Gql.Bases;

namespace Sig.App.Backend.Requests.Commands.Mutations.Transactions
{
    public class EditLoyaltyFundOnCard : IRequestHandler<EditLoyaltyFundOnCard.Input, EditLoyaltyFundOnCard.Payload>
    {
        private readonly ILogger<EditLoyaltyFundOnCard> logger;
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly IHttpContextAccessor httpContextAccessor;

        public EditLoyaltyFundOnCard(ILogger<EditLoyaltyFundOnCard> logger, AppDbContext db, IClock clock, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.db = db;
            this.clock = clock;
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Tentatives avant d'abandonner. Même raison que <c>CreateTransaction</c> : une vraie course
        /// se règle en un rejeu, une carte qui échoue trois fois de suite est martelée et boucler
        /// masquerait le problème.
        /// </summary>
        public const int MaxPlanningAttempts = 3;

        /// <summary>
        /// CRCL-2669 - Cette mutation fixe un solde ABSOLU (« mets la carte-cadeau à 50 »), elle ne
        /// déplace pas un montant. Elle ne peut donc pas passer par
        /// <c>SaveChangesWithFundRetryAsync</c> : le joint lit toute écriture comme un mouvement
        /// (voulu − lu) et la rejoue sur la valeur en base. Un achat de 30 qui s'insère entre la
        /// lecture (100) et l'écriture (50) ferait rebaser −50 sur 70, soit 20, alors que la
        /// transaction et le journal enregistrés annoncent 50 : le solde et le grand livre ne
        /// diraient plus la même chose.
        ///
        /// Le <c>SaveChanges</c> est donc nu, et c'est le jeton de concurrence sur <c>Fund.Amount</c>
        /// qui fait le travail : le « WHERE Amount = &lt;valeur lue&gt; » ne trouve pas sa ligne, EF
        /// lève, et l'édition est REPLANIFIÉE sur des données fraîches - le montant demandé est
        /// réappliqué tel quel, et le mouvement journalisé est recalculé depuis le solde réel.
        /// L'achat concurrent n'est ni effacé ni compté deux fois, et l'intention de l'admin est
        /// respectée : la carte vaut ce qu'il a demandé.
        /// </summary>
        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await HandleAttempt(request, cancellationToken);
                }
                catch (DbUpdateConcurrencyException exception) when (attempt < MaxPlanningAttempts)
                {
                    // Vider le suivi est ce qui force la relecture : une requête suivie rendrait la
                    // même instance périmée, et la tentative suivante reposerait sur le même solde.
                    logger.LogWarning($"[Mutation] EditLoyaltyFundOnCard - Solde modifié par une écriture concurrente, réédition sur des données fraîches (tentative {attempt} de {MaxPlanningAttempts}) : {exception.Message}");
                    db.ChangeTracker.Clear();
                }
            }
        }

        private async Task<Payload> HandleAttempt(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] EditLoyaltyFundOnCard({request.CardId}, {request.Amount})");

            if (request.Amount < 0)
            {
                logger.LogWarning("[Mutation] EditLoyaltyFundOnCard - LoyaltyFundCantBeNegativeException");
                throw new LoyaltyFundCantBeNegativeException();
            }

            var cardId = request.CardId.LongIdentifierForType<Card>();
            var card = await db
                .Cards.Include(x => x.Beneficiary).ThenInclude(x => x.Organization)
                .Include(x => x.Funds).ThenInclude(x => x.ProductGroup)
                .Include(x => x.Project)
                .FirstOrDefaultAsync(x => x.Id == cardId, cancellationToken);

            if (card == null)
            {
                logger.LogWarning("[Mutation] EditLoyaltyFundOnCard - CardNotFoundException");
                throw new CardNotFoundException();
            }

            var today = clock.GetCurrentInstant().ToDateTimeUtc();
            var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            var currentUser = db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId);

            var loyaltyProductGroup = db.ProductGroups.FirstOrDefault(x => x.Name == ProductGroupType.LOYALTY && x.ProjectId == card.ProjectId);

            var transactionUniqueId = TransactionHelper.CreateTransactionUniqueId();
            var transaction = new LoyaltyEditFundTransaction()
            {
                TransactionUniqueId = transactionUniqueId,
                Card = card,
                Amount = request.Amount - card.LoyaltyFund(),
                AvailableFund = request.Amount,
                CreatedAtUtc = clock.GetCurrentInstant().ToDateTimeUtc(),
                ProductGroup = loyaltyProductGroup
            };

            var transactionLogProductGroups = new List<TransactionLogProductGroup>();

            if (loyaltyProductGroup != null)
            {
                transactionLogProductGroups.Add(new TransactionLogProductGroup()
                {
                    Amount = transaction.Amount,
                    ProductGroupId = loyaltyProductGroup.Id,
                    ProductGroupName = loyaltyProductGroup.Name
                });
            }

            db.TransactionLogs.Add(new TransactionLog()
            {
                Discriminator = TransactionLogDiscriminator.LoyaltyEditFundTransactionLog,
                TransactionUniqueId = transactionUniqueId,
                CreatedAtUtc = today,
                TotalAmount = request.Amount - card.LoyaltyFund(),
                CardProgramCardId = card.ProgramCardId,
                CardNumber = card.CardNumber,
                BeneficiaryId = card.Beneficiary?.Id,
                BeneficiaryID1 = card.Beneficiary?.ID1,
                BeneficiaryID2 = card.Beneficiary?.ID2,
                BeneficiaryFirstname = card.Beneficiary?.Firstname,
                BeneficiaryLastname = card.Beneficiary?.Lastname,
                BeneficiaryEmail = card.Beneficiary?.Email,
                BeneficiaryPhone = card.Beneficiary?.Phone,
                BeneficiaryIsOffPlatform = card.Beneficiary is OffPlatformBeneficiary,
                BeneficiaryTypeId = card.Beneficiary?.BeneficiaryTypeId,
                OrganizationId = card.Beneficiary?.OrganizationId,
                OrganizationName = card.Beneficiary?.Organization.Name,
                TransactionInitiatorId = currentUserId,
                TransactionInitiatorFirstname = currentUser?.Profile.FirstName,
                TransactionInitiatorLastname = currentUser?.Profile.LastName,
                TransactionInitiatorEmail = currentUser?.Email,
                ProjectId = card.ProjectId,
                ProjectName = card.Project.Name,
                TransactionLogProductGroups = transactionLogProductGroups
            });
            db.Transactions.Add(transaction);

            var fund = card.Funds.FirstOrDefault(x => x.ProductGroup.Name == ProductGroupType.LOYALTY);
            if (fund == null)
            {
                if (loyaltyProductGroup == null)
                {
                    logger.LogWarning("[Mutation] EditLoyaltyFundOnCard - Project has no loyalty product group");
                    throw new CardIsNotGiftCardException();
                }

                fund = new Fund()
                {
                    Card = card,
                    ProductGroup = loyaltyProductGroup
                };

                db.Funds.Add(fund);
            }

            fund.Amount = request.Amount;

            if (fund.Amount == 0)
            {
                card.Funds.Remove(fund);
                db.Funds.Remove(fund);
                if (card.Beneficiary == null)
                {
                    card.Status = CardStatus.Unassigned;
                }
            }

            logger.LogInformation($"[Mutation] EditLoyaltyFundOnCard - Edit loyalty fund {request.Amount} to ({request.CardId}) card");

            // Nu, et non le joint : voir la note sur Handle. Le jeton sur Fund.Amount transforme une
            // écriture concurrente en DbUpdateConcurrencyException, que la boucle replanifie.
            await db.SaveChangesAsync(cancellationToken);

            return new Payload()
            {
                Transaction = new LoyaltyEditFundTransactionGraphType(transaction)
            };
        }

        [MutationInput]
        public class Input : HaveCardId, IRequest<Payload>
        {
            public decimal Amount { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public LoyaltyEditFundTransactionGraphType Transaction { get; set; }
        }

        public class CardNotFoundException : RequestValidationException { }
        public class CardIsNotGiftCardException : RequestValidationException { }
        public class LoyaltyFundCantBeNegativeException : RequestValidationException { }
    }
}
