using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetUsersByIds : BatchQuery<GetUsersByIds.Query, string, UserGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetUsersByIds(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<string, UserGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var users = await db.Users
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return users.ToDictionary(x => x.Id, x => new UserGraphType(x));
        }
    }
}