using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.Organizations
{
    public class GetOrganizationsStats : IRequestHandler<GetOrganizationsStats.Input, GetOrganizationsStats.Payload>
    {
        private readonly AppDbContext db;
        private readonly IClock clock;

        public GetOrganizationsStats(AppDbContext db, IClock clock)
        {
            this.db = db;
            this.clock = clock;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var organizations = await db.Organizations
                .Include(x => x.BudgetAllowances).ThenInclude(x => x.Subscription)
                .Where(x => x.ProjectId == projectId)
                .AsNoTracking()
                .ToListAsync();
            List<ManuallyAddingFundTransaction> manuallyAddingTransactions;
            List<SubscriptionAddingFundTransaction> subscriptionTransactions;
            List<ExpireFundTransaction> expiredTransactions;

            if (request.Subscriptions != null && request.Subscriptions.Any()) {
                manuallyAddingTransactions = await db.Transactions.OfType<ManuallyAddingFundTransaction>()
                    .Include(x => x.Transactions)
                    .Include(x => x.ExpireFundTransaction)
                    .Where(x => request.Subscriptions.Contains(x.SubscriptionId) && !x.Subscription.IsArchived)
                    .AsNoTracking()
                    .ToListAsync();

                subscriptionTransactions = await db.Transactions.OfType<SubscriptionAddingFundTransaction>()
                    .Include(x => x.Transactions)
                    .Include(x => x.ExpireFundTransaction)
                    .Include(x => x.SubscriptionType)
                    .Where(x => !x.SubscriptionType.Subscription.IsArchived)
                    .AsNoTracking()
                    .ToListAsync();
                subscriptionTransactions = subscriptionTransactions.Where(x => request.Subscriptions.Contains(x.SubscriptionType.SubscriptionId)).ToList();

                expiredTransactions = await db.Transactions.OfType<ExpireFundTransaction>()
                    .Include(x => x.ExpiredSubscription)
                    .Where(x => request.Subscriptions.Contains(x.ExpiredSubscriptionId) && !x.ExpiredSubscription.IsArchived)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var organization in organizations)
                {
                    organization.BudgetAllowances = organization.BudgetAllowances.Where(x => request.Subscriptions.Contains(x.SubscriptionId) && !x.Subscription.IsArchived).ToList();
                }
            }
            else
            {
                manuallyAddingTransactions = await db.Transactions.OfType<ManuallyAddingFundTransaction>()
                    .Include(x => x.Transactions)
                    .Include(x => x.ExpireFundTransaction)
                    .Where(x => !x.Subscription.IsArchived)
                    .AsNoTracking()
                    .ToListAsync();

                subscriptionTransactions = await db.Transactions.OfType<SubscriptionAddingFundTransaction>()
                    .Include(x => x.Transactions)
                    .Include(x => x.ExpireFundTransaction)
                    .Include(x => x.SubscriptionType)
                    .Where(x => !x.SubscriptionType.Subscription.IsArchived)
                    .AsNoTracking()
                    .ToListAsync();

                expiredTransactions = await db.Transactions.OfType<ExpireFundTransaction>()
                    .Include(x => x.ExpiredSubscription)
                    .Where(x => !x.ExpiredSubscription.IsArchived)
                    .AsNoTracking()
                    .ToListAsync();
            }

            var payload = new Payload
            {
                Items = new List<PayloadItem>()
            };

            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            foreach (var organization in organizations)
            {
                var organizationManuallyAddingTransactions = manuallyAddingTransactions.Where(x => x.OrganizationId == organization.Id).ToList();
                var organizationSubscriptionTransactions = subscriptionTransactions.Where(x => x.OrganizationId == organization.Id).ToList();
                var organizationExpiredTransactions = expiredTransactions.Where(x => x.OrganizationId == organization.Id).ToList();

                var totalActiveSubscriptionsEnvelopes = organization.BudgetAllowances.Where(x => x.Subscription.FundsExpirationDate >= today || !x.Subscription.IsFundsAccumulable && !x.Subscription.IsArchived).Sum(x => x.OriginalFund);

                payload.Items.Add(new PayloadItem()
                {
                    Organization = organization,
                    TotalActiveSubscriptionsEnvelopes = totalActiveSubscriptionsEnvelopes,
                    RemainingPerEnvelope = organization.BudgetAllowances.Where(x => x.Subscription.FundsExpirationDate >= today || !x.Subscription.IsFundsAccumulable && !x.Subscription.IsArchived).Sum(x => x.AvailableFund),
                    BalanceOnCards = GetBalanceOnCards(organizationManuallyAddingTransactions, organizationSubscriptionTransactions),
                    CardSpendingAmounts = GetCardSpendingAmounts(organizationManuallyAddingTransactions, organizationSubscriptionTransactions),
                    TotalAllocatedOnCards = GetTotalAllocatedOnCardsAmounts(organizationManuallyAddingTransactions, organizationSubscriptionTransactions),
                    ExpiredAmounts = GetCardExpiredAmounts(organizationExpiredTransactions)
                });
            }

            return payload;
        }

        public decimal GetBalanceOnCards(List<ManuallyAddingFundTransaction> manuallyAddingTransactions, List<SubscriptionAddingFundTransaction> subscriptionTransactions)
        {
            var manuallyAddingBalance = manuallyAddingTransactions.Where(x => x.CardId != null).Sum(x => x.AvailableFund);
            var subscriptionBalance = subscriptionTransactions.Where(x => x.CardId != null).Sum(x => x.AvailableFund);

            return manuallyAddingBalance + subscriptionBalance;
        }

        public decimal GetTotalAllocatedOnCardsAmounts(List<ManuallyAddingFundTransaction> manuallyAddingTransactions, List<SubscriptionAddingFundTransaction> subscriptionTransactions)
        {
            var manuallyAddingAmount = manuallyAddingTransactions.Where(x => x.CardId != null).Sum(x => x.Amount);
            var subscriptionAmount = subscriptionTransactions.Where(x => x.CardId != null).Sum(x => x.Amount);
            
            return manuallyAddingAmount + subscriptionAmount;
        }

        public decimal GetCardSpendingAmounts(List<ManuallyAddingFundTransaction> manuallyAddingTransactions, List<SubscriptionAddingFundTransaction> subscriptionTransactions)
        {
            decimal spendingAmounts = 0;
            foreach (var transaction in manuallyAddingTransactions)
            {
                if (transaction.Status != FundTransactionStatus.Expired )
                {
                    spendingAmounts += transaction.Amount - transaction.AvailableFund;
                }
                else
                {
                    if (transaction.ExpireFundTransaction != null)
                    {
                        spendingAmounts += transaction.Amount - transaction.ExpireFundTransaction.Amount;
                    }
                    else
                    {
                        spendingAmounts += transaction.Amount - transaction.AvailableFund;
                    }
                }
            }

            foreach (var transaction in subscriptionTransactions)
            {
                if (transaction.Status != FundTransactionStatus.Expired)
                {
                    spendingAmounts += transaction.Amount - transaction.AvailableFund;
                }
                else
                {
                    if (transaction.ExpireFundTransaction != null)
                    {
                        spendingAmounts += transaction.Amount - transaction.ExpireFundTransaction.Amount;
                    }
                    else
                    {
                        spendingAmounts += transaction.Amount - transaction.AvailableFund;
                    }
                }
            }

            return spendingAmounts;
        }

        public decimal GetCardExpiredAmounts(List<ExpireFundTransaction> transactions)
        {
            return transactions.Sum(x => x.Amount);
        }

        public class Input : HaveProjectId, IRequest<Payload>
        {
            public IEnumerable<long> Subscriptions { get; set; }
        }

        public class Payload
        {
            public List<PayloadItem> Items { get; set; }
        }

        public class PayloadItem
        {
            public Organization Organization { get; set; }
            public decimal TotalActiveSubscriptionsEnvelopes { get; set; }
            public decimal TotalAllocatedOnCards { get; set; }
            public decimal RemainingPerEnvelope { get; set; }
            public decimal BalanceOnCards { get; set; }
            public decimal CardSpendingAmounts { get; set; }
            public decimal ExpiredAmounts { get; set; }
        }
    }
}
