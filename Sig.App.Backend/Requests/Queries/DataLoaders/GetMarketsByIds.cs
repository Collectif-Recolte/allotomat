using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetMarketsByIds : BatchQuery<GetMarketsByIds.Query, long, MarketGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetMarketsByIds(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, MarketGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var markets = await db.Markets
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return markets.ToDictionary(x => x.Id, x => new MarketGraphType(x));
        }
    }
}