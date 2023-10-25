using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetOrganizationsByIds : BatchQuery<GetOrganizationsByIds.Query, long, OrganizationGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetOrganizationsByIds(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, OrganizationGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var organizations = await db.Organizations
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return organizations.ToDictionary(x => x.Id, x => new OrganizationGraphType(x));
        }
    }
}