using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetOrganizationByMarketId : BatchCollectionQuery<GetOrganizationByMarketId.Query, long, OrganizationGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetOrganizationByMarketId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, OrganizationGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.OrganizationMarkets
                .Include(x => x.Organization)
                .Where(x => request.Ids.Contains(x.MarketId))
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.MarketId, x => new OrganizationGraphType(x.Organization));
        }
    }
}
