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
    public class GetPaymentTransactionById : BatchQuery<GetPaymentTransactionById.Query, long, PaymentTransactionGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetPaymentTransactionById(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, PaymentTransactionGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var transactions = await db.Transactions
                .OfType<PaymentTransaction>()
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return transactions.ToDictionary(x => x.Id, x => new PaymentTransactionGraphType(x));
        }
    }
}