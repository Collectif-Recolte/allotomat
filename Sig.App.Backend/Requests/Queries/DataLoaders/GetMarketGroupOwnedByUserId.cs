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
    public class GetMarketGroupOwnedByUserId : BatchCollectionQuery<GetMarketGroupOwnedByUserId.Query, string, MarketGroupGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;

        public GetMarketGroupOwnedByUserId(UserManager<AppUser> userManager, AppDbContext db)
        {
            this.userManager = userManager;
            this.db = db;
        }

        public override async Task<ILookup<string, MarketGroupGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var manager = await db.Users
                .Where(c => request.Ids.Contains(c.Id))
                .FirstAsync();

            var existingClaims = await userManager.GetClaimsAsync(manager);

            switch (manager.Type)
            {
                case UserType.ProjectManager:
                {
                    var existingMarketClaims = existingClaims.Where(x => x.Type == AppClaimTypes.ProjectManagerOf).Select(x => x.Value);
                    var results = await db.MarketGroups.Where(x => existingMarketClaims.Contains(x.ProjectId.ToString())).ToListAsync();

                    return results.ToLookup(x => manager.Id, x => new MarketGroupGraphType(x));
                }
                case UserType.MarketGroupManager:
                {
                    var existingMarketGroupClaims = existingClaims.Where(x => x.Type == AppClaimTypes.MarketGroupManagerOf).Select(x => x.Value);
                    var results = await db.MarketGroups.Where(x => existingMarketGroupClaims.Contains(x.Id.ToString())).ToListAsync();

                    return results.ToLookup(x => manager.Id, x => new MarketGroupGraphType(x));
                }
            }

            return new MarketGroupGraphType[0].ToLookup(x => manager.Id, x => x);
        }
    }
}
