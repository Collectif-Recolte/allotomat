using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Services.Beneficiaries;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetBeneficiariesByBeneficiaryTypeId : BatchCollectionQuery<GetBeneficiariesByBeneficiaryTypeId.Query, long, BeneficiaryGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;
        private readonly IBeneficiaryService beneficiaryService;

        public GetBeneficiariesByBeneficiaryTypeId(AppDbContext db, IBeneficiaryService beneficiaryService)
        {
            this.db = db;
            this.beneficiaryService = beneficiaryService;
        }

        public override async Task<ILookup<long, BeneficiaryGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var canSeeAll = await beneficiaryService.CurrentUserCanSeeAllBeneficiaryInfo();

            var results = await db.Beneficiaries
                .Include(x => x.Organization).ThenInclude(x => x.Project)
                .Where(x => request.Ids.Contains(x.BeneficiaryTypeId.Value))
                .OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.BeneficiaryTypeId.Value, x => new BeneficiaryGraphType(x, beneficiaryService.ShouldAnonymizeBeneficiaries(x.Organization?.Project, canSeeAll)));
        }
    }
}
