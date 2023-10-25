using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetSubscriptionTypeByProductGroupId : BatchCollectionQuery<GetSubscriptionTypeByProductGroupId.Query, long, SubscriptionTypeGraphType>
    {
        public class Query : BaseQuery, IHaveGroup<long>
        {
            public long Group { get; set; }
        }

        private readonly AppDbContext db;

        public GetSubscriptionTypeByProductGroupId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, SubscriptionTypeGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.SubscriptionTypes
                .Where(c => request.Ids.Contains(c.ProductGroupId))
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.ProductGroupId, x => new SubscriptionTypeGraphType(x));
        }
    }
}