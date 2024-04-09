using System.Linq;
using Sig.App.Backend.Extensions;
using GraphQL.Conventions;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Services.Permission;
using Sig.App.Backend.Services.Permission.Enums;
using Sig.App.Backend.Gql.Bases;

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
        public CanManageUserRequirementHandler(PermissionService permissionService)
        {
            this.permissionService = permissionService;
        }
        
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
            CanManageUserRequirement requirement)
        {
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