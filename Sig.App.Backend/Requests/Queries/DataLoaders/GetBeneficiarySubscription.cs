using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetBeneficiarySubscriptionTypes : BatchCollectionQuery<GetBeneficiarySubscriptionTypes.Query, long, BeneficiarySubscriptionTypeGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetBeneficiarySubscriptionTypes(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, BeneficiarySubscriptionTypeGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.SubscriptionBeneficiaries.Include(x => x.Subscription).Include(x => x.Beneficiary).Where(x => request.Ids.Contains(x.BeneficiaryId)).ToListAsync();
            return results.ToLookup(x => x.BeneficiaryId, x => new BeneficiarySubscriptionTypeGraphType(x.Beneficiary, x.Subscription));
        }
    }
}