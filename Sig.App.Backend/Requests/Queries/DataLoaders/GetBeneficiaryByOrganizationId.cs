using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Services.Beneficiaries;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetBeneficiaryByOrganizationId : BatchCollectionQuery<GetBeneficiaryByOrganizationId.Query, long, IBeneficiaryGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;
        private readonly IBeneficiaryService beneficiaryService;

        public GetBeneficiaryByOrganizationId(AppDbContext db, IBeneficiaryService beneficiaryService)
        {
            this.db = db;
            this.beneficiaryService = beneficiaryService;
        }

        public override async Task<ILookup<long, IBeneficiaryGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var canSeeAll = await beneficiaryService.CurrentUserCanSeeAllBeneficiaryInfo();

            var results = await db.Beneficiaries
                .Include(x => x.Organization).ThenInclude(x => x.Project)
                .Where(x => request.Ids.Contains(x.OrganizationId))
                .OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.OrganizationId, x => {
                var isBeneficiariesAnonymous = beneficiaryService.ShouldAnonymizeBeneficiaries(x.Organization?.Project, canSeeAll);
                return x is OffPlatformBeneficiary opb ? new OffPlatformBeneficiaryGraphType(opb, isBeneficiariesAnonymous) as IBeneficiaryGraphType : new BeneficiaryGraphType(x, isBeneficiariesAnonymous);
            });
        }
    }
}
