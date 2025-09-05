using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetEmailOptInByUserIds : BatchQuery<GetEmailOptInByUserIds.Query, string, UserEmailOptInGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetEmailOptInByUserIds(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<string, UserEmailOptInGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.UserEmailOptIns
                .Where(x => request.Ids.Contains(x.UserId))
                .ToListAsync(cancellationToken);

            return results.ToDictionary(x => x.UserId, x => new UserEmailOptInGraphType(x));
        }
    }
}