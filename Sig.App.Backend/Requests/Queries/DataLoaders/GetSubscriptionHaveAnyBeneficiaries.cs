using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetSubscriptionHaveAnyBeneficiaries : BatchQuery<GetSubscriptionHaveAnyBeneficiaries.Query, long, bool>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetSubscriptionHaveAnyBeneficiaries(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, bool>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.Subscriptions
                .Include(x => x.Beneficiaries)
                .Where(x => request.Ids.Contains(x.Id))
                .ToListAsync();

            var groups = results.GroupBy(x => x.Id);

            return groups.ToDictionary(x => x.Key, x => x.Any(x => x.Beneficiaries.Any()));
        }
    }
}