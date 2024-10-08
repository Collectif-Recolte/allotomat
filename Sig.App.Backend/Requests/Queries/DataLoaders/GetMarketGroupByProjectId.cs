using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetMarketGroupByProjectId : BatchCollectionQuery<GetMarketGroupByProjectId.Query, long, MarketGroupGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetMarketGroupByProjectId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, MarketGroupGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.MarketGroups
                .Where(x => request.Ids.Contains(x.ProjectId))
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.ProjectId, x => new MarketGroupGraphType(x));
        }
    }
}
