using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetProjectsByIds : BatchQuery<GetProjectsByIds.Query, long, ProjectGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetProjectsByIds(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, ProjectGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var projects = await db.Projects
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return projects.ToDictionary(x => x.Id, x => new ProjectGraphType(x));
        }
    }
}