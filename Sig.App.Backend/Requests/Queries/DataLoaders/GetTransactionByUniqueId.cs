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
    public class GetTransactionByUniqueId : BatchQuery<GetTransactionByUniqueId.Query, string, ITransactionGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetTransactionByUniqueId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<string, ITransactionGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var budgetAllowances = await db.Transactions
                .Where(c => request.Ids.Contains(c.TransactionUniqueId))
                .ToListAsync(cancellationToken);

            return budgetAllowances.ToDictionary(x => x.TransactionUniqueId, x =>
            {
                switch (x)
                {
                    case null:
                        return null;
                    case PaymentTransaction pt:
                        return new PaymentTransactionGraphType(pt) as ITransactionGraphType;
                    case SubscriptionAddingFundTransaction aft:
                        return new SubscriptionAddingFundTransactionGraphType(aft) as ITransactionGraphType;
                    case ManuallyAddingFundTransaction maft:
                        return new ManuallyAddingFundTransactionGraphType(maft) as ITransactionGraphType;
                    case LoyaltyAddingFundTransaction laft:
                        return new LoyaltyAddingFundTransactionGraphType(laft) as ITransactionGraphType;
                    case RefundTransaction rft:
                        return new RefundTransactionGraphType(rft) as ITransactionGraphType;
                    case OffPlatformAddingFundTransaction opaft:
                        return new OffPlatformAddingFundTransactionGraphType(opaft) as ITransactionGraphType;
                }

                return new TransactionGraphType(x) as ITransactionGraphType;
            });
        }
    }
}