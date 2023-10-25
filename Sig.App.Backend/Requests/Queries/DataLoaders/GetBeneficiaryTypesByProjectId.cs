using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetBeneficiaryTypesByProjectId : BatchCollectionQuery<GetBeneficiaryTypesByProjectId.Query, long, BeneficiaryTypeGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetBeneficiaryTypesByProjectId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, BeneficiaryTypeGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.BeneficiaryTypes
                .Where(x => request.Ids.Contains(x.ProjectId))
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.ProjectId, x => new BeneficiaryTypeGraphType(x));
        }
    }
}
