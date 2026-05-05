using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetMarketGroupTransactions : BatchCollectionQuery<GetMarketGroupTransactions.Query, long, ITransactionGraphType>
    {
        public class Query : BaseQuery {}

        private readonly AppDbContext db;

        public GetMarketGroupTransactions(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, ITransactionGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var cashRegisterMarketGroups = await db.CashRegisterMarketGroups
                .Where(x => request.Ids.Contains(x.MarketGroupId))
                .ToListAsync(cancellationToken);

            var cashRegisterIds = cashRegisterMarketGroups.Select(x => x.CashRegisterId).Distinct().ToList();

            var paymentTransactions = await db.Transactions.OfType<PaymentTransaction>()
                .Where(c => c.CashRegisterId.HasValue && cashRegisterIds.Contains(c.CashRegisterId.Value))
                .ToListAsync(cancellationToken);

            var refundTransactions = await db.Transactions.OfType<RefundTransaction>()
                .Include(x => x.InitialTransaction)
                .Where(c => c.InitialTransaction.CashRegisterId.HasValue && cashRegisterIds.Contains(c.InitialTransaction.CashRegisterId.Value))
                .ToListAsync(cancellationToken);

            var cashRegisterToGroupIds = cashRegisterMarketGroups
                .GroupBy(x => x.CashRegisterId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.MarketGroupId).ToList());

            var allTransactions = paymentTransactions.Cast<Transaction>()
                .Concat(refundTransactions.Cast<Transaction>())
                .OrderByDescending(x => x.CreatedAtUtc);

            var results = allTransactions.SelectMany(t =>
            {
                var cashRegisterId = t is PaymentTransaction pt ? pt.CashRegisterId!.Value : ((RefundTransaction)t).InitialTransaction.CashRegisterId!.Value;
                ITransactionGraphType graphType = t is PaymentTransaction pt2
                    ? new PaymentTransactionGraphType(pt2)
                    : new RefundTransactionGraphType((RefundTransaction)t);
                return cashRegisterToGroupIds[cashRegisterId].Select(mgId => (mgId, graphType));
            });

            return results.ToLookup(x => x.mgId, x => x.graphType);
        }
    }
}
