using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetProjectByMarketId : BatchCollectionQuery<GetProjectByMarketId.Query, long, ProjectGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetProjectByMarketId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, ProjectGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.ProjectMarkets
                .Include(x => x.Project)
                .Where(x => request.Ids.Contains(x.MarketId))
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.MarketId, x => new ProjectGraphType(x.Project));
        }
    }
}
