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
    public class GetAddingFundTransactionById : BatchQuery<GetAddingFundTransactionById.Query, long, IAddingFundTransactionGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetAddingFundTransactionById(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long, IAddingFundTransactionGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var transactions = await db.Transactions
                .OfType<AddingFundTransaction>()
                .Where(c => request.Ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            return transactions.ToDictionary(x => x.Id, x =>
            {
                switch (x)
                {
                    case ManuallyAddingFundTransaction maft:
                        return new ManuallyAddingFundTransactionGraphType(maft);
                    case SubscriptionAddingFundTransaction saft:
                        return new SubscriptionAddingFundTransactionGraphType(saft);
                    case OffPlatformAddingFundTransaction opaft:
                        return new OffPlatformAddingFundTransactionGraphType(opaft) as IAddingFundTransactionGraphType;
                }

                return null;
            });
        }
    }
}