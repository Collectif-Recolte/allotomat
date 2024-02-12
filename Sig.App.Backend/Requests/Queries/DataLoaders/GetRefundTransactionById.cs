using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetRefundTransactionById : BatchQuery<GetRefundTransactionById.Query, long, RefundTransactionGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetRefundTransactionById(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, RefundTransactionGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var budgetAllowances = await db.Transactions
                .OfType<RefundTransaction>()
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return budgetAllowances.ToDictionary(x => x.Id, x => new RefundTransactionGraphType(x));
        }
    }
}