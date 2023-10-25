using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetSubscriptionTypeByBeneficiaryAndSubscriptionId : BatchQuery<GetSubscriptionTypeByBeneficiaryAndSubscriptionId.Query, long, SubscriptionTypeGraphType>
    {
        public class Query : BaseQuery, IHaveGroup<long>
        {
            public long Group { get; set; }
        }

        private readonly AppDbContext db;

        public GetSubscriptionTypeByBeneficiaryAndSubscriptionId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, SubscriptionTypeGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var beneficiary = await db.Beneficiaries.FirstAsync(x => x.Id == request.Group);

            var types = await db.SubscriptionTypes
                .Where(c => request.Ids.Contains(c.SubscriptionId) && c.BeneficiaryTypeId == beneficiary.BeneficiaryTypeId)
                .ToListAsync(cancellationToken);

            return types.ToDictionary(x => x.SubscriptionId, x => new SubscriptionTypeGraphType(x));
        }
    }
}