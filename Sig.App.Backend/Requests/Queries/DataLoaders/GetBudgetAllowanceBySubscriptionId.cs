using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetBudgetAllowanceBySubscriptionId : BatchCollectionQuery<GetBudgetAllowanceBySubscriptionId.Query, long, BudgetAllowanceGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetBudgetAllowanceBySubscriptionId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, BudgetAllowanceGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.BudgetAllowances
                .Where(x => request.Ids.Contains(x.SubscriptionId))
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.SubscriptionId, x => new BudgetAllowanceGraphType(x));
        }
    }
}
