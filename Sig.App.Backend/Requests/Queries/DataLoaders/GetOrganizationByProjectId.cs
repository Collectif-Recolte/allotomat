using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetOrganizationByProjectId : BatchCollectionQuery<GetOrganizationByProjectId.Query, long, OrganizationGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetOrganizationByProjectId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, OrganizationGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.Organizations
                .Where(x => request.Ids.Contains(x.ProjectId))
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.ProjectId, x => new OrganizationGraphType(x));
        }
    }
}
