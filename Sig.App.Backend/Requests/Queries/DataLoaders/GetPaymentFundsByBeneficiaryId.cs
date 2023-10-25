using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetPaymentFundsByBeneficiaryId : BatchCollectionQuery<GetPaymentFundsByBeneficiaryId.Query, long, PaymentFundGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetPaymentFundsByBeneficiaryId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, PaymentFundGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.PaymentFunds.Where(x => request.Ids.Contains(x.BeneficiaryId)).ToListAsync();
            return results.ToLookup(x => x.BeneficiaryId, x => new PaymentFundGraphType(x));
        }
    }
}