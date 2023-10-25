using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetOrganizationBudgetAllowanceTotal : BatchQuery<GetOrganizationBudgetAllowanceTotal.Query, long, decimal>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetOrganizationBudgetAllowanceTotal(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, decimal>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.BudgetAllowances
                .Where(x => request.Ids.Contains(x.OrganizationId))
                .ToListAsync();

            var groups = results.GroupBy(x => x.OrganizationId);

            return groups.ToDictionary(x => x.Key, x => x.Sum(x => x.AvailableFund));
        }
    }
}