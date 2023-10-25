using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetMarketOwnedByUserId : BatchCollectionQuery<GetMarketOwnedByUserId.Query, string, MarketGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;

        public GetMarketOwnedByUserId(UserManager<AppUser> userManager, AppDbContext db)
        {
            this.userManager = userManager;
            this.db = db;
        }

        public override async Task<ILookup<string, MarketGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var manager = await db.Users
                .Where(c => request.Ids.Contains(c.Id))
                .FirstAsync();

            var existingClaims = await userManager.GetClaimsAsync(manager);

            switch (manager.Type)
            {
                case UserType.Merchant:
                {
                    var existingMarketClaims = existingClaims.Where(x => x.Type == AppClaimTypes.MarketManagerOf).Select(x => x.Value);
                    var results = await db.Markets.Where(x => !x.IsArchived && existingMarketClaims.Contains(x.Id.ToString())).ToListAsync();

                    return results.ToLookup(x => manager.Id, x => new MarketGraphType(x));
                }
            }

            return new MarketGraphType[0].ToLookup(x => manager.Id, x => x);
        }
    }
}
