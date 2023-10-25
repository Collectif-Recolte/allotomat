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
    public class GetOrganizationOwnedByUserId : BatchCollectionQuery<GetOrganizationOwnedByUserId.Query, string, OrganizationGraphType>
    {
        public class Query : BaseQuery { }

        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;

        public GetOrganizationOwnedByUserId(UserManager<AppUser> userManager, AppDbContext db)
        {
            this.userManager = userManager;
            this.db = db;
        }

        public override async Task<ILookup<string, OrganizationGraphType>> Handle(Query request, CancellationToken cancellationToken)
        {
            var manager = await db.Users
                .Where(c => request.Ids.Contains(c.Id))
                .FirstAsync();

            var existingClaims = await userManager.GetClaimsAsync(manager);

            switch (manager.Type)
            {
                case UserType.ProjectManager:
                {
                    var existingProjectClaims = existingClaims.Where(x => x.Type == AppClaimTypes.ProjectManagerOf).Select(x => x.Value);
                    var results = await db.Projects.Include(x => x.Organizations).Where(x => existingProjectClaims.Contains(x.Id.ToString())).SelectMany(x => x.Organizations).OrderBy(x => x.Name).ToListAsync();

                    return results.ToLookup(x => manager.Id, x => new OrganizationGraphType(x));
                }
                case UserType.OrganizationManager:
                {
                    var existingOrganizationsClaims = existingClaims.Where(x => x.Type == AppClaimTypes.OrganizationManagerOf).Select(x => x.Value);

                    var results = await db.Organizations.Where(x => existingOrganizationsClaims.Contains(x.Id.ToString())).OrderBy(x => x.Name).ToListAsync();

                    return results.ToLookup(x => manager.Id, x => new OrganizationGraphType(x));
                }
            }

            return new OrganizationGraphType[0].ToLookup(x => manager.Id, x => x);
        }
    }
}
