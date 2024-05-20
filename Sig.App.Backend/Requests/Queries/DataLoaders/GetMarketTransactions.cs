using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetMarketTransactions : BatchCollectionQuery<GetMarketTransactions.Query, long, ITransactionGraphType>
    {
        public class Query : BaseQuery {}

        private readonly AppDbContext db;

        public GetMarketTransactions(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, ITransactionGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var paymentTransactions = await db.Transactions.OfType<PaymentTransaction>().Where(c => request.Ids.Contains(c.MarketId)).ToListAsync(cancellationToken);
            var refundTransactions = await db.Transactions.OfType<RefundTransaction>().Where(c => request.Ids.Contains(c.InitialTransaction.MarketId)).ToListAsync(cancellationToken);

            var results = paymentTransactions.OfType<Transaction>().Concat(refundTransactions.OfType<Transaction>());

            return results.OrderByDescending(x => x.CreatedAtUtc).ToLookup(x => x is PaymentTransaction ? (x as PaymentTransaction).MarketId : (x as RefundTransaction).InitialTransaction.MarketId,
                x => x is PaymentTransaction ? new PaymentTransactionGraphType(x as PaymentTransaction) as ITransactionGraphType : new RefundTransactionGraphType(x as RefundTransaction));
        }
    }
}