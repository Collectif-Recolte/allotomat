using MediatR;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.Projects
{
    public class SearchProjectSubscriptionEndReport : IRequestHandler<SearchProjectSubscriptionEndReport.Query, SubscriptionEndReportPagination<SubscriptionEndReportGraphType>>
    {
        private readonly AppDbContext db;

        public SearchProjectSubscriptionEndReport(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<SubscriptionEndReportPagination<SubscriptionEndReportGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            IQueryable<TransactionLog> query = db.TransactionLogs.Where(x => x.OrganizationId != null && x.ProjectId == request.ProjectId && x.CreatedAtUtc >= request.StartDate && x.CreatedAtUtc <= request.EndDate);

            if (request.Subscriptions != null && request.Subscriptions.Count() > 0)
            {
                query = query.Where(x => request.Subscriptions.Contains(x.SubscriptionId.Value));
            }

            if (request.Organizations != null && request.Organizations.Count() > 0)
            {
                query = query.Where(x => request.Organizations.Contains(x.OrganizationId.Value));
            }

            var transactionLogs = await query.AsNoTracking().ToListAsync();
            var transactionLogsGroupBy = transactionLogs.GroupBy(x => x.OrganizationId);
            var organizations = await db.Organizations.Where(x => transactionLogsGroupBy.Select(x => x.Key).Contains(x.Id)).AsNoTracking().ToListAsync();
            var subscriptionIds = transactionLogs.Select(x => x.SubscriptionId).Distinct();
            var subscriptions = await db.Subscriptions.Where(x => subscriptionIds.Any(y => y == x.Id)).AsNoTracking().ToListAsync();

            var result = SubscriptionEndReportPagination.For(transactionLogsGroupBy.Select(x =>
            {
                var transactionBySubscription = x.Select(x => x).Where(x => x.SubscriptionId.HasValue).GroupBy(x => x.SubscriptionId.Value);
                var reportBySubscription = transactionBySubscription.Select(y =>
                {
                    var subscription = subscriptions.FirstOrDefault(z => z.Id == y.Key);
                    var transactions = y.ToList();

                    return new SubscriptionEndTransactionGraphType()
                    {
                        Subscription = new SubscriptionGraphType(subscription),
                        TotalPurchases = transactions.Where(z => z.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog).Count(),
                        CardsWithFunds = transactions.Where(z => z.Discriminator == TransactionLogDiscriminator.ManuallyAddingFundTransactionLog || z.Discriminator == TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog).DistinctBy(z => z.CardNumber).Count(),
                        CardsUsedForPurchases = transactions.Where(z => z.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog).DistinctBy(z => z.CardNumber).Count(),
                        MerchantsWithPurchases = transactions.Where(z => z.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog).DistinctBy(z => z.MarketId).Count(),
                        TotalFundsLoaded = transactions.Where(z => z.Discriminator == TransactionLogDiscriminator.ManuallyAddingFundTransactionLog || z.Discriminator == TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog).Sum(z => z.TotalAmount),
                        TotalPurchaseValue = transactions.Where(z => z.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog).Sum(z => z.TotalAmount) - transactions.Where(z => z.Discriminator == TransactionLogDiscriminator.RefundPaymentTransactionLog).Sum(z => z.TotalAmount),
                        TotalExpiredAmount = transactions.Where(z => z.Discriminator == TransactionLogDiscriminator.ExpireFundTransactionLog).Sum(z => z.TotalAmount)
                    };
                });

                return new SubscriptionEndReportGraphType { Organization = new OrganizationGraphType(organizations.First(y => y.Id == x.Key)), SubscriptionEndTransactions = reportBySubscription };
            }).OrderBy(x => x.Organization.Id), request.Page);

            result.Total = new SubscriptionEndReportTotalGraphType()
            {
                TotalPurchases = transactionLogs.Where(z => z.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog).Count(),
                CardsWithFunds = transactionLogs.Where(z => z.Discriminator == TransactionLogDiscriminator.ManuallyAddingFundTransactionLog || z.Discriminator == TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog).DistinctBy(z => z.CardNumber).Count(),
                CardsUsedForPurchases = transactionLogs.Where(z => z.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog).DistinctBy(z => z.CardNumber).Count(),
                MerchantsWithPurchases = transactionLogs.Where(z => z.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog).DistinctBy(z => z.MarketId).Count(),
                TotalFundsLoaded = transactionLogs.Where(z => z.Discriminator == TransactionLogDiscriminator.ManuallyAddingFundTransactionLog || z.Discriminator == TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog).Sum(z => z.TotalAmount),
                TotalPurchaseValue = transactionLogs.Where(z => z.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog).Sum(z => z.TotalAmount) - transactionLogs.Where(z => z.Discriminator == TransactionLogDiscriminator.RefundPaymentTransactionLog).Sum(z => z.TotalAmount),
                TotalExpiredAmount = transactionLogs.Where(z => z.Discriminator == TransactionLogDiscriminator.ExpireFundTransactionLog).Sum(z => z.TotalAmount)
            };

            return result;
        }

        public class Query : IRequest<SubscriptionEndReportPagination<SubscriptionEndReportGraphType>>
        {
            public Page Page { get; set; }
            public long ProjectId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public IEnumerable<long> Subscriptions { get; set; }
            public IEnumerable<long> Organizations { get; set; }
        }
    }
}
