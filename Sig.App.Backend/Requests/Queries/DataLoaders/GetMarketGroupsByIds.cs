using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetMarketGroupsByIds : BatchQuery<GetMarketGroupsByIds.Query, long, MarketGroupGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetMarketGroupsByIds(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, MarketGroupGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var markets = await db.MarketGroups
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return markets.ToDictionary(x => x.Id, x => new MarketGroupGraphType(x));
        }
    }
}