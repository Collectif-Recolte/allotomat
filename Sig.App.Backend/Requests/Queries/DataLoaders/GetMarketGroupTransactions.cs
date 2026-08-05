using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetMarketGroupTransactions : BatchCollectionQuery<GetMarketGroupTransactions.Query, long, ITransactionGraphType>
    {
        public class Query : BaseQuery, IHaveGroup<TransactionFilter>
        {
            public TransactionFilter Group { get; set; }
        }

        private readonly AppDbContext db;

        public GetMarketGroupTransactions(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, ITransactionGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var startUtc = request.Group.StartDate.ToDateTimeUtc();
            var endUtc = request.Group.EndDate.ToDateTimeUtc();

            var logsQuery = db.TransactionLogs.Where(x =>
                x.MarketGroupId.HasValue
                && request.Ids.Contains(x.MarketGroupId.Value)
                && x.CreatedAtUtc >= startUtc
                && x.CreatedAtUtc < endUtc
                && (x.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog
                    || x.Discriminator == TransactionLogDiscriminator.RefundPaymentTransactionLog));

            if (request.Group.CashRegisterIds.Length > 0)
            {
                logsQuery = logsQuery.Where(x =>
                    x.CashRegisterId.HasValue && request.Group.CashRegisterIds.Contains(x.CashRegisterId.Value));
            }

            var logs = await logsQuery.ToListAsync(cancellationToken);
            var uniqueIds = logs.Select(x => x.TransactionUniqueId).Distinct().ToList();

            var paymentTransactions = await db.Transactions.OfType<PaymentTransaction>()
                .Where(c => uniqueIds.Contains(c.TransactionUniqueId))
                .ToListAsync(cancellationToken);

            var refundTransactions = await db.Transactions.OfType<RefundTransaction>()
                .Include(x => x.InitialTransaction)
                .Where(c => uniqueIds.Contains(c.TransactionUniqueId))
                .ToListAsync(cancellationToken);

            var paymentsByUniqueId = paymentTransactions.ToDictionary(x => x.TransactionUniqueId);
            var refundsByUniqueId = refundTransactions.ToDictionary(x => x.TransactionUniqueId);

            var results = logs
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(log =>
                {
                    ITransactionGraphType graphType = null;
                    if (log.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog
                        && paymentsByUniqueId.TryGetValue(log.TransactionUniqueId, out var payment))
                    {
                        graphType = new PaymentTransactionGraphType(payment);
                    }
                    else if (log.Discriminator == TransactionLogDiscriminator.RefundPaymentTransactionLog
                        && refundsByUniqueId.TryGetValue(log.TransactionUniqueId, out var refund))
                    {
                        graphType = new RefundTransactionGraphType(refund);
                    }

                    return (MarketGroupId: log.MarketGroupId!.Value, GraphType: graphType);
                })
                .Where(x => x.GraphType != null);

            return results.ToLookup(x => x.MarketGroupId, x => x.GraphType);
        }
    }
}
