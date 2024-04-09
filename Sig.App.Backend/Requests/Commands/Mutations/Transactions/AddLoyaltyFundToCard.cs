using System.Collections.Generic;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Projects;
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
    public class AddLoyaltyFundToCard : IRequestHandler<AddLoyaltyFundToCard.Input, AddLoyaltyFundToCard.Payload>
    {
        private readonly ILogger<AddLoyaltyFundToCard> logger;
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly IHttpContextAccessor httpContextAccessor;

        public AddLoyaltyFundToCard(ILogger<AddLoyaltyFundToCard> logger, AppDbContext db, IClock clock, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.db = db;
            this.clock = clock;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AddLoyaltyFundToCard({request.ProjectId}, {request.CardId}, {request.Amount})");
            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var card = await db.Cards.Include(x => x.Beneficiary).ThenInclude(x => x.Organization).Include(x => x.Transactions).Include(x => x.Funds)
                .ThenInclude(x => x.ProductGroup).Include(x => x.Project)
                .FirstOrDefaultAsync(x => x.ProgramCardId == request.CardId && x.ProjectId == projectId,
                    cancellationToken);

            if (card == null)
            {
                logger.LogWarning("[Mutation] AddLoyaltyFundToCard - CardNotFoundException");
                throw new CardNotFoundException();
            }

            if (card.Status == CardStatus.Lost)
            {
                logger.LogWarning("[Mutation] AddLoyaltyFundToCard - CardLostException");
                throw new CardLostException();
            }

            var today = clock.GetCurrentInstant().InUtc().ToDateTimeUtc();
            var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            var currentUser = db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId);

            var loyaltyProductGroup = db.ProductGroups.FirstOrDefault(x => x.Name == ProductGroupType.LOYALTY && x.ProjectId == card.ProjectId);

            var transactionUniqueId = TransactionHelper.CreateTransactionUniqueId();
            var transaction = new LoyaltyAddingFundTransaction()
            {
                TransactionUniqueId = transactionUniqueId,
                Card = card,
                Amount = request.Amount,
                AvailableFund = request.Amount,
                CreatedAtUtc = clock.GetCurrentInstant().InUtc().ToDateTimeUtc(),
                ProductGroup = loyaltyProductGroup
            };
            card.Transactions.Add(transaction);

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
                Discriminator = TransactionLogDiscriminator.LoyaltyAddingFundTransactionLog,
                TransactionUniqueId = transactionUniqueId,
                CreatedAtUtc = today,
                TotalAmount = transaction.Amount,
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

            var fund = card.Funds.FirstOrDefault(x => x.ProductGroup.Name == ProductGroupType.LOYALTY);

            if (fund == null)
            {
                fund = new Fund()
                {
                    Card = card,
                    ProductGroup = loyaltyProductGroup
                };

                db.Funds.Add(fund);
            }

            fund.Amount += request.Amount;
            
            if (card.Status == CardStatus.Unassigned)
            {
                card.Status = CardStatus.GiftCard;
            }

            logger.LogInformation($"[Mutation] AddLoyaltyFundToCard - Adding loyalty fund {request.Amount} to ({request.CardId}) card");

            await db.SaveChangesAsync();

            return new Payload()
            {
                Transaction = new LoyaltyAddingFundTransactionGraphType(transaction)
            };
        }

        [MutationInput]
        public class Input : HaveProjectId, IRequest<Payload>
        {
            public long CardId { get; set; }
            public decimal Amount { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public LoyaltyAddingFundTransactionGraphType Transaction { get; set; }
        }

        public class CardNotFoundException : RequestValidationException { }
        public class CardLostException : RequestValidationException { }
    }
}
