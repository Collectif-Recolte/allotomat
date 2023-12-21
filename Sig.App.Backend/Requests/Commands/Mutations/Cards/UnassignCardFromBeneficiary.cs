using System.Collections.Generic;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.Helpers;

namespace Sig.App.Backend.Requests.Commands.Mutations.Cards
{
    public class UnassignCardFromBeneficiary : IRequestHandler<UnassignCardFromBeneficiary.Input, UnassignCardFromBeneficiary.Payload>
    {
        private readonly ILogger<UnassignCardFromBeneficiary> logger;
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly IHttpContextAccessor httpContextAccessor;

        public UnassignCardFromBeneficiary(ILogger<UnassignCardFromBeneficiary> logger, AppDbContext db, IClock clock, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.db = db;
            this.clock = clock;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            long beneficiaryId;
            if (request.BeneficiaryId.IsIdentifierForType(typeof(Beneficiary)))
            {
                beneficiaryId = request.BeneficiaryId.LongIdentifierForType<Beneficiary>();
            }
            else
            {
                beneficiaryId = request.BeneficiaryId.LongIdentifierForType<OffPlatformBeneficiary>();
            }
            var beneficiary = await db.Beneficiaries.Include(x => x.Organization).ThenInclude(x => x.Project).FirstOrDefaultAsync(x => x.Id == beneficiaryId, cancellationToken);

            if (beneficiary == null) throw new BeneficiaryNotFoundException();

            var cardId = request.CardId.LongIdentifierForType<Card>();
            var card = await db.Cards.Include(x => x.Beneficiary).Include(x => x.Transactions).Include(x => x.Funds).ThenInclude(x => x.ProductGroup).FirstOrDefaultAsync(x => x.Id == cardId, cancellationToken);

            if (card == null) throw new CardNotFoundException();

            if (card.Beneficiary == null || card.Beneficiary.Id != beneficiaryId) throw new CardNotAssignToBeneficiaryException();
            
            var today = clock.GetCurrentInstant().InUtc().ToDateTimeUtc();
            var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            var currentUser = db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId);

            var loyaltyFund = card.Funds.FirstOrDefault(x => x.ProductGroup.Name == ProductGroupType.LOYALTY);
            if (loyaltyFund == null || loyaltyFund.Amount == 0)
            {
                card.Status = CardStatus.Unassigned;
            }
            else
            {
                card.Status = CardStatus.GiftCard;
            }
            
            foreach (var fund in card.Funds)
            {
                fund.Card = null;
                fund.CardId = null;
                fund.Amount = 0;
            }

            if (card.Transactions.Any())
            {
                var addingFundTransactions = card.Transactions.OfType<AddingFundTransaction>()
                    .Where(x => x is not LoyaltyAddingFundTransaction)
                    .Where(x => x.Status == FundTransactionStatus.Actived && x.AvailableFund > 0).ToList();

                var addingFundTransactionsBySubscriptionId =
                    await TransactionHelper.GroupAddingFundTransactionsBySubscriptionId(db, addingFundTransactions,
                        cancellationToken);
                var subscriptions = await db.Subscriptions
                    .Where(x => addingFundTransactionsBySubscriptionId.Select(y => y.Key).Contains(x.Id))
                    .ToListAsync(cancellationToken);
                var productGroups = await db.ProductGroups
                    .Where(x => addingFundTransactions.Select(y => y.ProductGroupId).Contains(x.Id))
                    .ToListAsync(cancellationToken);
                
                foreach (var transaction in card.Transactions)
                {
                    if (transaction is IExpiringFundTransaction eft and not LoyaltyAddingFundTransaction)
                    {
                        eft.Status = FundTransactionStatus.Unassigned;
                    }

                    transaction.Card = null;
                    transaction.CardId = null;
                }

                foreach (var group in addingFundTransactionsBySubscriptionId)
                {
                    var budgetAllowance = db.BudgetAllowances.FirstOrDefault(x =>
                        x.SubscriptionId == group.Key && x.OrganizationId == beneficiary.OrganizationId);
                    var subscription = subscriptions.FirstOrDefault(x => x.Id == group.Key);

                    if (budgetAllowance != null)
                    {
                        var refundAmount = group.Sum(x => x.AvailableFund);
                        budgetAllowance.AvailableFund += refundAmount;
                        
                        var transactionLogProductGroups = new List<TransactionLogProductGroup>();
                        foreach (var productGroup in group.ToList().GroupBy(x => x.ProductGroupId))
                        {
                            var currentProductGroup = productGroups.First(x => x.Id == productGroup.First().ProductGroupId);
                            transactionLogProductGroups.Add(new TransactionLogProductGroup()
                            {
                                Amount = productGroup.Sum(x => x.AvailableFund),
                                ProductGroupId = currentProductGroup.Id,
                                ProductGroupName = currentProductGroup.Name
                            });
                        }
                        
                        db.TransactionLogs.Add(new TransactionLog()
                        {
                            Discriminator = TransactionLogDiscriminator
                                .RefundBudgetAllowanceFromUnassignedCardTransactionLog,
                            CreatedAtUtc = today,
                            TotalAmount = refundAmount,
                            CardProgramCardId = card.ProgramCardId,
                            CardNumber = card.CardNumber,
                            BeneficiaryId = beneficiary.Id,
                            BeneficiaryID1 = beneficiary.ID1,
                            BeneficiaryID2 = beneficiary.ID2,
                            BeneficiaryFirstname = beneficiary.Firstname,
                            BeneficiaryLastname = beneficiary.Lastname,
                            BeneficiaryEmail = beneficiary.Email,
                            BeneficiaryPhone = beneficiary.Phone,
                            BeneficiaryIsOffPlatform = beneficiary is OffPlatformBeneficiary,
                            BeneficiaryTypeId = beneficiary.BeneficiaryTypeId,
                            OrganizationId = beneficiary.OrganizationId,
                            OrganizationName = beneficiary.Organization.Name,
                            SubscriptionId = subscription?.Id,
                            SubscriptionName = subscription?.Name,
                            ProjectId = beneficiary.Organization.ProjectId,
                            ProjectName = beneficiary.Organization.Project.Name,
                            TransactionLogProductGroups = transactionLogProductGroups,
                            TransactionInitiatorId = currentUserId,
                            TransactionInitiatorFirstname = currentUser?.Profile.FirstName,
                            TransactionInitiatorLastname = currentUser?.Profile.LastName,
                            TransactionInitiatorEmail = currentUser?.Email
                        });
                    }
                }
            }

            beneficiary.Card = null;
            beneficiary.CardId = null;
            card.Beneficiary = null;

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"Card ({card.Id}) unassign from {beneficiary.Firstname} {beneficiary.Lastname} ({beneficiary.Id})");

            return new Payload() {
                Beneficiary = beneficiary is OffPlatformBeneficiary opb ? new OffPlatformBeneficiaryGraphType(opb) : new BeneficiaryGraphType(beneficiary)
            };
        }

        [MutationInput]
        public class Input : IRequest<Payload>, IHaveBeneficiaryId, IHaveCardId
        {
            public Id BeneficiaryId { get; set; }
            public Id CardId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public IBeneficiaryGraphType Beneficiary { get; set; }
        }

        public class CardNotFoundException : RequestValidationException { }
        public class BeneficiaryNotFoundException : RequestValidationException { }
        public class CardNotAssignToBeneficiaryException : RequestValidationException { }
    }
}
