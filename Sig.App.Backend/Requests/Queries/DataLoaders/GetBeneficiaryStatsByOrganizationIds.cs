using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class LoadBeneficiaryStatsByOrganizationIds : BatchQuery<LoadBeneficiaryStatsByOrganizationIds.Query, long, BeneficiaryStatsGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public LoadBeneficiaryStatsByOrganizationIds(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, BeneficiaryStatsGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.Organizations
                .Include(x => x.Beneficiaries)
                .ThenInclude(x => x.Subscriptions)
                .Where(x => request.Ids.Contains(x.Id))
                .AsNoTracking()
                .ToDictionaryAsync(x => x.Id);

            return results.ToDictionary(x => x.Key, x =>
            {
                return new BeneficiaryStatsGraphType()
                {
                    BeneficiariesWithoutCard = x.Value.Beneficiaries.Where(x => x.CardId == null && (x.Subscriptions.Count > 0 || (x is OffPlatformBeneficiary && (x as OffPlatformBeneficiary).IsActive))).Count(),
                };
            });
        }
    }
}
