using System.Linq;
using Sig.App.Backend.Extensions;
using GraphQL.Conventions;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Services.Permission;
using Sig.App.Backend.Services.Permission.Enums;
using Sig.App.Backend.Gql.Bases;
using Microsoft.AspNetCore.Identity;

namespace Sig.App.Backend.Authorization.Requirements
{
    public class CanManageUserRequirement : IAuthorizationRequirement, IDescribedRequirement
    {
        public string Describe()
        {
            return "Current user is Admin, or is the user being accessed.";
        }
    }

    public class CanManageUserRequirementHandler : AuthorizationHandler<CanManageUserRequirement>
    {
        private PermissionService permissionService;
        private UserManager<AppUser> userManager;
        public CanManageUserRequirementHandler(PermissionService permissionService, UserManager<AppUser> userManager)
        {
            this.permissionService = permissionService;
            this.userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
            CanManageUserRequirement requirement)
        {
            // Même garde que RequirePermissionAttribute: un compte désactivé n'a droit à rien ici,
            // peu importe la permission ou le chemin (admin ou gestion de soi-même).
            var currentUser = await userManager.FindByIdAsync(context.User.GetUserId());
            if (currentUser?.Status != UserStatus.Actived)
            {
                return;
            }

            var globalPermissions = await permissionService.GetGlobalPermissions(context.User);
            if (globalPermissions.Contains(GlobalPermission.ManageAllUsers))
            {
                context.Succeed(requirement);
            }
            else
            {
                string userId;

                switch (context.Resource)
                {
                    case IResolutionContext ctx when ctx.Source is UserGraphType ugt:
                        userId = ugt.Id.IdentifierForType<AppUser>();
                        break;
                    case IResolutionContext ctx when ctx.GetInputValue() is HaveUserId hui:
                        userId = hui.UserId.IdentifierForType<AppUser>();
                        break;
                    case Id id when id.IsIdentifierForType<AppUser>():
                        userId = id.IdentifierForType<AppUser>();
                        break;
                    default:
                        return;
                }

                if (context.User.GetUserId() == userId)
                {
                    context.Succeed(requirement);
                }
            }
            
            return;
        }
    }
}