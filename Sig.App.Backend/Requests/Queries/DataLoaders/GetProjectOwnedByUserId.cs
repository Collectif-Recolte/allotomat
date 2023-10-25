using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public class GetProjectOwnedByUserId : BatchCollectionQuery<GetProjectOwnedByUserId.Query, string, ProjectGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;

        public GetProjectOwnedByUserId(UserManager<AppUser> userManager, AppDbContext db)
        {
            this.userManager = userManager;
            this.db = db;
        }

        public override async Task<ILookup<string, ProjectGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var manager = await db.Users
                .Where(c => request.Ids.Contains(c.Id))
                .FirstAsync();

            var existingClaims = await userManager.GetClaimsAsync(manager);
            var existingProjectClaims = existingClaims.Where(x => x.Type == AppClaimTypes.ProjectManagerOf).Select(x => x.Value);

            var results = await db.Projects.Where(x => existingProjectClaims.Contains(x.Id.ToString())).ToListAsync();

            return results.ToLookup(x => manager.Id, x => new ProjectGraphType(x));
        }
    }
}
