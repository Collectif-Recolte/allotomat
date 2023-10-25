using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetBeneficiaryTypeByBeneficiaryId : BatchQuery<GetBeneficiaryTypeByBeneficiaryId.Query, long, BeneficiaryTypeGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetBeneficiaryTypeByBeneficiaryId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, BeneficiaryTypeGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var markets = await db.Beneficiaries.Include(x => x.BeneficiaryType)
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return markets.ToDictionary(x => x.Id, x => x.BeneficiaryType != null ? new BeneficiaryTypeGraphType(x.BeneficiaryType) : null);
        }
    }
}