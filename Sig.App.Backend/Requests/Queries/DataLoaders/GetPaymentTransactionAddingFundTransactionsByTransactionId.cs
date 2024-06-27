using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetPaymentTransactionAddingFundTransactionsByTransactionId : BatchCollectionQuery<GetPaymentTransactionAddingFundTransactionsByTransactionId.Query, long, PaymentTransactionAddingFundTransactionGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetPaymentTransactionAddingFundTransactionsByTransactionId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, PaymentTransactionAddingFundTransactionGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.PaymentTransactionAddingFundTransactions.Include(x => x.AddingFundTransaction).Include(x => x.PaymentTransaction).Where(x => request.Ids.Contains(x.PaymentTransactionId)).ToListAsync();
            return results.ToLookup(x => x.PaymentTransactionId, x => new PaymentTransactionAddingFundTransactionGraphType(x));
        }
    }
}