using System.Linq;
using FluentAssertions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Xunit;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Plugins.Identity;
using Sig.App.Backend.Services.Permission;
using Sig.App.Backend.Services.Permission.Enums;

namespace Sig.App.BackendTests.Services
{
    public class PermissionsServiceTests : TestBase
    {
        private readonly AppUser admin;
        private readonly AppUser user1;
        private readonly AppUserClaimsPrincipalFactory claimsPrincipalFactory;
        private readonly PermissionService permissionService;

        public PermissionsServiceTests()
        {
            admin = AddUser("admin@example.com", UserType.PCAAdmin, password: "Abcd1234!!");
            user1 = AddUser("user1@example.com", UserType.OrganizationManager, password: "Abcd1234!!");
            
            claimsPrincipalFactory = new AppUserClaimsPrincipalFactory(
                UserManager,
                new OptionsWrapper<IdentityOptions>(UserManager.Options));
            permissionService = new PermissionService(DbContext);
        }

        [Fact]
        public async Task AdminCanManageAllUsers()
        {
            var permission = GlobalPermission.ManageAllUsers;
            var claimPrincipal = await claimsPrincipalFactory.CreateAsync(admin);
            
            var permissions = await permissionService.GetGlobalPermissions(claimPrincipal);

            permissions.Contains(permission).Should().BeTrue();
        }
        
        [Fact]
        public async Task UserCanNotManageAllUsers()
        {
            var permission = GlobalPermission.ManageAllUsers;
            var claimPrincipal = await claimsPrincipalFactory.CreateAsync(user1);

            var permissions = await permissionService.GetGlobalPermissions(claimPrincipal);

            permissions.Contains(permission).Should().BeFalse();
        }
    }
}