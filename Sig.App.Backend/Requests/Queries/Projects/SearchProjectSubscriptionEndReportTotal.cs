using MediatR;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.Projects
{
    public class SearchProjectSubscriptionEndReportTotal : IRequestHandler<SearchProjectSubscriptionEndReportTotal.Query, SubscriptionEndReportTotalGraphType>
    {
        private readonly AppDbContext db;

        public SearchProjectSubscriptionEndReportTotal(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<SubscriptionEndReportTotalGraphType> Handle(Query request, CancellationToken cancellationToken)
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

            return new SubscriptionEndReportTotalGraphType()
            {
                TotalPurchases = transactionLogs.Where(z => z.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog).Count(),
                CardsWithFunds = transactionLogs.Where(z => z.Discriminator == TransactionLogDiscriminator.ManuallyAddingFundTransactionLog || z.Discriminator == TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog).DistinctBy(z => z.CardNumber).Count(),
                CardsUsedForPurchases = transactionLogs.DistinctBy(z => z.CardNumber).Count(),
                MerchantsWithPurchases = transactionLogs.DistinctBy(z => z.MarketId).Count(),
                TotalFundsLoaded = transactionLogs.Where(z => z.Discriminator == TransactionLogDiscriminator.ManuallyAddingFundTransactionLog || z.Discriminator == TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog).Sum(z => z.TotalAmount),
                TotalPurchaseValue = transactionLogs.Where(z => z.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog).Sum(z => z.TotalAmount) - transactionLogs.Where(z => z.Discriminator == TransactionLogDiscriminator.RefundPaymentTransactionLog).Sum(z => z.TotalAmount),
                TotalExpiredAmount = transactionLogs.Where(z => z.Discriminator == TransactionLogDiscriminator.ExpireFundTransactionLog).Sum(z => z.TotalAmount)
            };
        }

        public class Query : IRequest<SubscriptionEndReportTotalGraphType>
        {
            public long ProjectId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public IEnumerable<long> Subscriptions { get; set; }
            public IEnumerable<long> Organizations { get; set; }
        }
    }
}
