using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetBudgetAllowance : BatchQuery<GetBudgetAllowance.Query, long, BudgetAllowanceGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetBudgetAllowance(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, BudgetAllowanceGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var budgetAllowances = await db.BudgetAllowances
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return budgetAllowances.ToDictionary(x => x.Id, x => new BudgetAllowanceGraphType(x));
        }
    }
}