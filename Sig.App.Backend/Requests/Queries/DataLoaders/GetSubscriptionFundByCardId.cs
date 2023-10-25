using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetSubscriptionFundByCardId : BatchCollectionQuery<GetSubscriptionFundByCardId.Query, long?, FundGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetSubscriptionFundByCardId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<ILookup<long?, FundGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.Funds.Where(x => request.Ids.Contains(x.CardId) && x.ProductGroup.Name != ProductGroupType.LOYALTY).ToListAsync();
            return results.ToLookup(x => x.CardId, x => new FundGraphType(x));
        }
    }
}