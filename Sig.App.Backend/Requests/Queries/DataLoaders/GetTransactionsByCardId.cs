using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Transactions;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetTransactionsByCardId : BatchCollectionQuery<GetTransactionsByCardId.Query, long, Transaction>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetTransactionsByCardId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, Transaction>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.Transactions.Where(c => c.CardId.HasValue && request.Ids.Contains(c.CardId.Value)).ToListAsync(cancellationToken);

            return results.ToLookup(x => x.CardId.Value, x => x);
        }
    }
}
