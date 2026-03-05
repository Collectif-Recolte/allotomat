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

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
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
                logger.LogWarning("[Mutation] EditLoyaltyFundOnCard - Card has no loyalty fund (not a gift card)");
                throw new CardIsNotGiftCardException();
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

            await db.SaveChangesAsync();

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
