using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Services.Beneficiaries;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetBeneficiaryByIds : BatchQuery<GetBeneficiaryByIds.Query, long, IBeneficiaryGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;
        private readonly IBeneficiaryService beneficiaryService;

        public GetBeneficiaryByIds(AppDbContext db, IBeneficiaryService beneficiaryService)
        {
            this.db = db;
            this.beneficiaryService = beneficiaryService;
        }

        public override async Task<IDictionary<long, IBeneficiaryGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var canSeeAll = await beneficiaryService.CurrentUserCanSeeAllBeneficiaryInfo();

            var markets = await db.Beneficiaries
                .Include(x => x.Organization).ThenInclude(x => x.Project)
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return markets.ToDictionary(x => x.Id, x => {
                var isBeneficiariesAnonymous = !canSeeAll && (x.Organization?.Project?.BeneficiariesAreAnonymous ?? true);
                if (x is OffPlatformBeneficiary opb)
                {
                    return new OffPlatformBeneficiaryGraphType(opb, isBeneficiariesAnonymous) as IBeneficiaryGraphType;
                }
                return new BeneficiaryGraphType(x, isBeneficiariesAnonymous);
            });
        }
    }
}
