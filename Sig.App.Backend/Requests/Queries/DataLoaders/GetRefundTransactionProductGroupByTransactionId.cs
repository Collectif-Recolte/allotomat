using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetRefundTransactionProductGroupByTransactionId : BatchCollectionQuery<GetRefundTransactionProductGroupByTransactionId.Query, long, RefundTransactionProductGroupGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetRefundTransactionProductGroupByTransactionId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, RefundTransactionProductGroupGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.RefundTransactionProductGroups.Where(x => request.Ids.Contains(x.RefundTransactionId)).ToListAsync();
            return results.ToLookup(x => x.RefundTransactionId, x => new RefundTransactionProductGroupGraphType(x));
        }
    }
}