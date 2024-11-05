using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetMarketGroupByCashRegisterId : BatchCollectionQuery<GetMarketGroupByCashRegisterId.Query, long, MarketGroupGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetMarketGroupByCashRegisterId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, MarketGroupGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.CashRegisterMarketGroups
                .Include(x => x.MarketGroup)
                .Where(x => request.Ids.Contains(x.CashRegisterId))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.CashRegisterId, x => new MarketGroupGraphType(x.MarketGroup));
        }
    }
}
