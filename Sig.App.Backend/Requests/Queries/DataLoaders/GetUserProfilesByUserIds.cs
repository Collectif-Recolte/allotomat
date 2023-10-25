using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetUserProfilesByUserIds : BatchQuery<GetUserProfilesByUserIds.Query, string, IProfileGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetUserProfilesByUserIds(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<string, IProfileGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.UserProfiles
                .Where(x => request.Ids.Contains(x.UserId))
                .ToListAsync(cancellationToken);

            return results.ToDictionary(x => x.UserId, IProfileGraphType.Create);
        }
    }
}