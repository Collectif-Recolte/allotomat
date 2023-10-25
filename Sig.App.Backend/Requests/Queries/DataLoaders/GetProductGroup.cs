using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetProductGroup : BatchQuery<GetProductGroup.Query, long, ProductGroupGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetProductGroup(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, ProductGroupGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var productGroups = await db.ProductGroups
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return productGroups.ToDictionary(x => x.Id, x => new ProductGroupGraphType(x));
        }
    }
}