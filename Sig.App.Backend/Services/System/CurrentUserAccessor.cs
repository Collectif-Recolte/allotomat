using Sig.App.Backend.Extensions;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;

namespace Sig.App.Backend.Services.System
{
    public class CurrentUserAccessor : ICurrentUserAccessor
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly AppDbContext db;

        public CurrentUserAccessor(IHttpContextAccessor httpContextAccessor, AppDbContext db)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.db = db;
        }

        public string GetCurrentUserId()
        {
            return GetPrincipal().GetUserId();
        }

        public bool IsUserType(UserType type)
        {
            return GetPrincipal().HasClaim(AppClaimTypes.UserType, type.ToString());
        }

        public ValueTask<AppUser> GetCurrentUser() => db.Users.FindAsync(GetCurrentUserId());

        private ClaimsPrincipal GetPrincipal() => httpContextAccessor.HttpContext.User;
    }
}
