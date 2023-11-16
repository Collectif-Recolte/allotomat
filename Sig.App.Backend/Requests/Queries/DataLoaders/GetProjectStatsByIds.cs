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

            var results = await db.Projects
                .Include(x => x.Organizations).ThenInclude(x => x.Beneficiaries).ThenInclude(x => x.Card).ThenInclude(x => x.Funds).ThenInclude(x => x.ProductGroup)
                .Include(x => x.Subscriptions).ThenInclude(x => x.BudgetAllowances)
                .Where(c => request.Ids.Contains(c.Id))
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id, x => x);

            return results.ToDictionary(x => x.Key, x =>
            {
                return new ProjectStatsGraphType()
                {
                    BeneficiaryCount = x.Value.Organizations.SelectMany(y => y.Beneficiaries).Count(),
                    UnspentLoyaltyFund = x.Value.Organizations.Sum(o => o.Beneficiaries.Where(b => b.Card != null).Sum(b => b.Card.Funds.Where(f => f.ProductGroup.Name == ProductGroupType.LOYALTY).Sum(f => f.Amount))),
                    TotalActiveSubscriptionsEnvelopes = x.Value.Subscriptions.Where(x => x.FundsExpirationDate >= today || !x.IsFundsAccumulable).Sum(y => y.BudgetAllowances.Sum(z => z.OriginalFund)),
                };
            });
        }
    }
}
