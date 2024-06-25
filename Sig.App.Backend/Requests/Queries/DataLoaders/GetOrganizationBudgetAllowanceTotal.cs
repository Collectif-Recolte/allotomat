using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Sig.App.Backend.DbModel;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetOrganizationBudgetAllowanceTotal : BatchQuery<GetOrganizationBudgetAllowanceTotal.Query, long, decimal>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;
        private readonly IClock clock;

        public GetOrganizationBudgetAllowanceTotal(AppDbContext db, IClock clock)
        {
            this.db = db;
            this.clock = clock;
        }

        public override async Task<IDictionary<long, decimal>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.BudgetAllowances
                .Include(x => x.Subscription)
                .Where(x => request.Ids.Contains(x.OrganizationId))
                .ToListAsync();

            var now = clock.GetCurrentInstant().ToDateTimeUtc();
            var groups = results.Where(x => x.Subscription.GetExpirationDate(clock) > now).GroupBy(x => x.OrganizationId);

            return groups.ToDictionary(x => x.Key, x => x.Sum(x => x.AvailableFund));
        }
    }
}