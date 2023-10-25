using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetLoyaltyFundByCardId : BatchQuery<GetLoyaltyFundByCardId.Query, long?, FundGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;

        public GetLoyaltyFundByCardId(AppDbContext db)
        {
            this.db = db;
        }

        public override async Task<IDictionary<long?, FundGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var results = await db.Funds
                .Where(c => request.Ids.Contains(c.CardId.Value) && c.ProductGroup.Name == ProductGroupType.LOYALTY)
                .ToListAsync(cancellationToken);

            return results.ToDictionary(x => x.CardId, x => new FundGraphType(x));
        }
    }
}