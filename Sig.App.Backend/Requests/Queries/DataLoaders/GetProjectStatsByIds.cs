using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Sig.App.Backend.DbModel.Enums;
using NodaTime;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{

    public class GetProjectStatsByIds : BatchQuery<GetProjectStatsByIds.Query, long, ProjectStatsGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;
        private readonly IClock clock;

        public GetProjectStatsByIds(AppDbContext db, IClock clock)
        {
            this.db = db;
            this.clock = clock;
        }

        public override async Task<IDictionary<long, ProjectStatsGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            var results = new Dictionary<long, ProjectStatsGraphType>();

            foreach (var id in request.Ids)
            {
                var beneficiaryCount = await db.Beneficiaries.Where(x => id == x.Organization.ProjectId).AsNoTracking().CountAsync();
                var unspentLoyaltyFund = await db.Funds.Where(x => id == x.Card.ProjectId && x.ProductGroup.Name == ProductGroupType.LOYALTY).AsNoTracking().SumAsync(x => x.Amount);
                var subscriptions = await db.Subscriptions.Include(x => x.BudgetAllowances).Where(x => id == x.ProjectId && !x.IsArchived).AsNoTracking().ToListAsync();
                var totalActiveSubscriptionsEnvelopes = subscriptions.Where(x => (x.FundsExpirationDate >= today || !x.IsFundsAccumulable)).Sum(x => x.BudgetAllowances.Sum(y => y.OriginalFund));
                results.Add(id, new ProjectStatsGraphType()
                {
                    BeneficiaryCount = beneficiaryCount,
                    UnspentLoyaltyFund = unspentLoyaltyFund,
                    TotalActiveSubscriptionsEnvelopes = totalActiveSubscriptionsEnvelopes
                });
            }

            return results;
        }
    }
}
