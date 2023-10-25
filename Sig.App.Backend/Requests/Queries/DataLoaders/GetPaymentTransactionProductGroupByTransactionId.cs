using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetPaymentTransactionProductGroupByTransactionId : BatchCollectionQuery<GetPaymentTransactionProductGroupByTransactionId.Query, long, PaymentTransactionProductGroupGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetPaymentTransactionProductGroupByTransactionId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, PaymentTransactionProductGroupGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.PaymentTransactionProductGroups.Where(x => request.Ids.Contains(x.PaymentTransactionId)).ToListAsync();
            return results.ToLookup(x => x.PaymentTransactionId, x => new PaymentTransactionProductGroupGraphType(x));
        }
    }
}