using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetBeneficiaryByOrganizationId : BatchCollectionQuery<GetBeneficiaryByOrganizationId.Query, long, IBeneficiaryGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetBeneficiaryByOrganizationId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, IBeneficiaryGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.Beneficiaries
                .Include(x => x.Organization).ThenInclude(x => x.Project)
                .Where(x => request.Ids.Contains(x.OrganizationId))
                .OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.OrganizationId, x => {
                var isBeneficiariesAnonymous = x.Organization?.Project?.BeneficiariesAreAnonymous ?? false;
                return x is OffPlatformBeneficiary opb ? new OffPlatformBeneficiaryGraphType(opb, isBeneficiariesAnonymous) as IBeneficiaryGraphType : new BeneficiaryGraphType(x, isBeneficiariesAnonymous);
            });
        }
    }
}
