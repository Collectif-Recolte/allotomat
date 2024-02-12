using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetTransactionByUniqueId : BatchQuery<GetTransactionByUniqueId.Query, string, TransactionGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetTransactionByUniqueId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<string, TransactionGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var budgetAllowances = await db.Transactions
                .Where(c => request.Ids.Contains(c.TransactionUniqueId))
                .ToListAsync(cancellationToken);

            return budgetAllowances.ToDictionary(x => x.TransactionUniqueId, x => new TransactionGraphType(x));
        }
    }
}