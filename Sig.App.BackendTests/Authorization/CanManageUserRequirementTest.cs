using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Sig.App.Backend.Authorization.Requirements;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Services.Permission;
using Xunit;

namespace Sig.App.BackendTests.Authorization
{
    public class CanManageUserRequirementTest : TestBase
    {
        private readonly CanManageUserRequirementHandler handler;

        public CanManageUserRequirementTest()
        {
            handler = new CanManageUserRequirementHandler(new PermissionService(DbContext), UserManager);
        }

        private async Task<bool> Authorize(AppUser currentUser, object resource)
        {
            var context = new AuthorizationHandlerContext(
                new[] { new CanManageUserRequirement() },
                CreatePrincipal(currentUser),
                resource);

            await handler.HandleAsync(context);

            return context.HasSucceeded;
        }

        [Fact]
        public async Task RefusesADisabledAccountEvenWithManageAllUsers()
        {
            var admin = AddUser("admin@example.com", UserType.PCAAdmin);
            admin.Status = UserStatus.Disabled;
            await DbContext.SaveChangesAsync();

            var target = AddUser("target@example.com", UserType.OrganizationManager);

            var succeeded = await Authorize(admin, target.GetIdentifier());

            succeeded.Should().BeFalse();
        }

        [Fact]
        public async Task AllowsAnActiveAccountWithManageAllUsers()
        {
            var admin = AddUser("admin@example.com", UserType.PCAAdmin);
            var target = AddUser("target@example.com", UserType.OrganizationManager);

            var succeeded = await Authorize(admin, target.GetIdentifier());

            succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task AllowsAnActiveAccountWithoutManageAllUsersToManageItself()
        {
            var user = AddUser("user@example.com", UserType.OrganizationManager);

            var succeeded = await Authorize(user, user.GetIdentifier());

            succeeded.Should().BeTrue();
        }

        [Fact]
        public async Task RefusesADisabledAccountWithoutManageAllUsersEvenToManageItself()
        {
            var user = AddUser("user@example.com", UserType.OrganizationManager);
            user.Status = UserStatus.Disabled;
            await DbContext.SaveChangesAsync();

            var succeeded = await Authorize(user, user.GetIdentifier());

            succeeded.Should().BeFalse();
        }
    }
}
