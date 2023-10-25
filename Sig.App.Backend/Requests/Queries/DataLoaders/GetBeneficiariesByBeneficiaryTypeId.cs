using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetBeneficiariesByBeneficiaryTypeId : BatchCollectionQuery<GetBeneficiariesByBeneficiaryTypeId.Query, long, BeneficiaryGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetBeneficiariesByBeneficiaryTypeId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, BeneficiaryGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.Beneficiaries
                .Where(x => request.Ids.Contains(x.BeneficiaryTypeId.Value))
                .OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.BeneficiaryTypeId.Value, x => new BeneficiaryGraphType(x));
        }
    }
}
