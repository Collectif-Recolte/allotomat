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
    public class GetMarketTransactions : BatchCollectionQuery<GetMarketTransactions.Query, long, PaymentTransactionGraphType>
    {
        public class Query : BaseQuery {}

        private readonly AppDbContext db;

        public GetMarketTransactions(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, PaymentTransactionGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.Transactions.OfType<PaymentTransaction>().Where(c => request.Ids.Contains(c.MarketId)).ToListAsync(cancellationToken);
            return results.ToLookup(x => x.MarketId, x => new PaymentTransactionGraphType(x));
        }
    }
}
