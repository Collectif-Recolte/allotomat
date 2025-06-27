using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetSubscriptionTransactionsByBeneficiaryAndSubscriptionId : BatchCollectionQuery<GetSubscriptionTransactionsByBeneficiaryAndSubscriptionId.Query, long, TransactionGraphType>
    {
        public class Query : BaseQuery, IHaveGroup<long>
        {
            public long Group { get; set; }
        }

        private readonly AppDbContext db;

        public GetSubscriptionTransactionsByBeneficiaryAndSubscriptionId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long, TransactionGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var transactions = await db.Transactions.OfType<SubscriptionAddingFundTransaction>()
                .Include(x => x.SubscriptionType)
                .Where(x => x.BeneficiaryId == request.Group && x.Status != DbModel.Enums.FundTransactionStatus.Unassigned && request.Ids.Contains(x.SubscriptionType.SubscriptionId)).ToListAsync();

            return transactions.ToLookup(x => x.SubscriptionType.SubscriptionId, x => new TransactionGraphType(x));
        }
    }
}