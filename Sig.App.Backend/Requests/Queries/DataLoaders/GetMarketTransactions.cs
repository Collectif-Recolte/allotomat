using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetMarketTransactions : BatchCollectionQuery<GetMarketTransactions.Query, long, ITransactionGraphType>
    {
        public class Query : BaseQuery, IHaveGroup<TransactionFilter>
        {
            public TransactionFilter Group { get; set; }
        }

        private readonly AppDbContext db;

        public GetMarketTransactions(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, ITransactionGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var startUtc = request.Group.StartDate.ToDateTimeUtc();
            var endUtc = request.Group.EndDate.ToDateTimeUtc();

            var cashRegisterIds = request.Group.CashRegisterIds;

            var paymentQuery = db.Transactions.OfType<PaymentTransaction>()
                .Where(c => request.Ids.Contains(c.MarketId) && c.CreatedAtUtc >= startUtc && c.CreatedAtUtc < endUtc);
            if (cashRegisterIds.Length > 0)
                paymentQuery = paymentQuery.Where(c => c.CashRegisterId.HasValue && cashRegisterIds.Contains(c.CashRegisterId.Value));
            var paymentTransactions = await paymentQuery.ToListAsync(cancellationToken);

            var refundQuery = db.Transactions.OfType<RefundTransaction>()
                .Where(c => request.Ids.Contains(c.InitialTransaction.MarketId) && c.CreatedAtUtc >= startUtc && c.CreatedAtUtc < endUtc);
            if (cashRegisterIds.Length > 0)
                refundQuery = refundQuery.Where(c => c.InitialTransaction.CashRegisterId.HasValue && cashRegisterIds.Contains(c.InitialTransaction.CashRegisterId.Value));
            var refundTransactions = await refundQuery.ToListAsync(cancellationToken);

            var results = paymentTransactions.OfType<Transaction>().Concat(refundTransactions.OfType<Transaction>());

            return results.OrderByDescending(x => x.CreatedAtUtc).ToLookup(x => x is PaymentTransaction ? (x as PaymentTransaction).MarketId : (x as RefundTransaction).InitialTransaction.MarketId,
                x => x is PaymentTransaction ? new PaymentTransactionGraphType(x as PaymentTransaction) as ITransactionGraphType : new RefundTransactionGraphType(x as RefundTransaction));
        }
    }
}