using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetCashRegistersByMarketId : BatchCollectionQuery<GetCashRegistersByMarketId.Query, long, CashRegisterGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetCashRegistersByMarketId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, CashRegisterGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.CashRegisters
                .Where(x => request.Ids.Contains(x.MarketId))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.MarketId, x => new CashRegisterGraphType(x));
        }
    }
}
