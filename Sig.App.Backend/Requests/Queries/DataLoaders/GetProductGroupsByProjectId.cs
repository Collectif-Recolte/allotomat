using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetProductGroupsByProjectId : BatchCollectionQuery<GetProductGroupsByProjectId.Query, long, ProductGroupGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetProductGroupsByProjectId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, ProductGroupGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.ProductGroups
                .Where(x => request.Ids.Contains(x.ProjectId))
                .ToListAsync(cancellationToken);

            return results.ToLookup(x => x.ProjectId, x => new ProductGroupGraphType(x));
        }
    }
}
