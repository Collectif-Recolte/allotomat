using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetSubscriptionByIds : BatchQuery<GetSubscriptionByIds.Query, long, SubscriptionGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetSubscriptionByIds(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, SubscriptionGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var users = await db.Subscriptions
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return users.ToDictionary(x => x.Id, x => new SubscriptionGraphType(x));
        }
    }
}
