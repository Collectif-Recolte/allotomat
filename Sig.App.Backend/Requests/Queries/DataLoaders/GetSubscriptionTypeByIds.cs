using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetSubscriptionTypeByIds : BatchQuery<GetSubscriptionTypeByIds.Query, long, SubscriptionTypeGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetSubscriptionTypeByIds(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, SubscriptionTypeGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var cards = await db. SubscriptionTypes
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return cards.ToDictionary(x => x.Id, x => new SubscriptionTypeGraphType(x));
        }
    }
}