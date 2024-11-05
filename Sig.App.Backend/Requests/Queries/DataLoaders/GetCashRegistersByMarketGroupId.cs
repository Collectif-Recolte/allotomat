using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetCashRegistersByMarketGroupId : BatchCollectionQuery<GetCashRegistersByMarketGroupId.Query, long, CashRegisterGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetCashRegistersByMarketGroupId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, CashRegisterGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.CashRegisterMarketGroups
                .Where(x => request.Ids.Contains(x.MarketGroupId))
                .Include(x => x.CashRegister)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.MarketGroupId, x => new CashRegisterGraphType(x.CashRegister));
        }
    }
}
