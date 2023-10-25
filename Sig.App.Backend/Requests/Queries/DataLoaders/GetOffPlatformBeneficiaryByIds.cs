using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetOffPlatformBeneficiaryByIds : BatchQuery<GetOffPlatformBeneficiaryByIds.Query, long, OffPlatformBeneficiaryGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetOffPlatformBeneficiaryByIds(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, OffPlatformBeneficiaryGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var markets = await db.Beneficiaries
                .Where(c => request.Ids.Contains(c.Id))
                .Select(x => x as OffPlatformBeneficiary)
                .ToListAsync(cancellationToken);

            return markets.ToDictionary(x => x.Id, x => new OffPlatformBeneficiaryGraphType(x));
        }
    }
}