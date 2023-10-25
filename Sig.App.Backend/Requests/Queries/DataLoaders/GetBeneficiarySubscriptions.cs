using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetBeneficiarySubscriptions : BatchCollectionQuery<GetBeneficiarySubscriptions.Query, long, SubscriptionGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetBeneficiarySubscriptions(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, SubscriptionGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.SubscriptionBeneficiaries.Include(x => x.Subscription).Where(x => request.Ids.Contains(x.BeneficiaryId)).ToListAsync();
            return results.ToLookup(x => x.BeneficiaryId, x => new SubscriptionGraphType(x.Subscription));
        }
    }
}