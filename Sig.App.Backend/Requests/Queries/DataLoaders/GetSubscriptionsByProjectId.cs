using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetSubscriptionsByProjectId : BatchCollectionQuery<GetSubscriptionsByProjectId.Query, long, SubscriptionGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetSubscriptionsByProjectId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, SubscriptionGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.Subscriptions
                .Where(x => request.Ids.Contains(x.ProjectId))
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.ProjectId, x => new SubscriptionGraphType(x));
        }
    }
}
