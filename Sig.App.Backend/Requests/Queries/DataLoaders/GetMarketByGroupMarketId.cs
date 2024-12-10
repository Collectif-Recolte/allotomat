using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetMarketByGroupMarketId : BatchCollectionQuery<GetMarketByGroupMarketId.Query, long, MarketGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetMarketByGroupMarketId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, MarketGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.MarketGroupMarkets
                .Include(x => x.Market)
                .Where(x => request.Ids.Contains(x.MarketGroupId))
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.MarketGroupId, x => new MarketGraphType(x.Market));
        }
    }
}
