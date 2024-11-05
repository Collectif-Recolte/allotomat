using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetCashRegistersByIds : BatchQuery<GetCashRegistersByIds.Query, long, CashRegisterGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetCashRegistersByIds(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, CashRegisterGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var markets = await db.CashRegisters
                .Where(c => request.Ids.Contains(c.Id))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return markets.ToDictionary(x => x.Id, x => new CashRegisterGraphType(x));
        }
    }
}