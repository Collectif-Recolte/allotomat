using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetTransactionLogProductGroupByTransactionLogId : BatchCollectionQuery<GetTransactionLogProductGroupByTransactionLogId.Query, long, TransactionLogProductGroupGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetTransactionLogProductGroupByTransactionLogId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, TransactionLogProductGroupGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.TransactionLogProductGroups.Where(x => request.Ids.Contains(x.TransactionLogId)).ToListAsync(cancellationToken);
            return results.ToLookup(x => x.TransactionLogId, x => new TransactionLogProductGroupGraphType(x));
        }
    }
}