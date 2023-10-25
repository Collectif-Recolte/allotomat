using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Sig.App.Backend.Constants;
using Sig.App.Backend.Controllers;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Plugins.Identity;
using Sig.App.Backend.Services.Permission;
using Sig.App.Backend.Services.Permission.Enums;
using Sig.App.BackendTests.TestHelpers;

namespace Sig.App.BackendTests.Controllers
{
    public class AccountControllerTests : TestBase
    {
        private readonly AccountController controller;
        private readonly AppUser user;
        private readonly Mock<IAuthenticationService> authenticationServiceMock;
        private readonly AppUserClaimsPrincipalFactory claimsPrincipalFactory;
        private readonly PermissionService permissionService;

        private const string Username = "test@example.com";
        private const string Password = "1234aAuuuuuu!";

        public AccountControllerTests()
        {
            claimsPrincipalFactory = new AppUserClaimsPrincipalFactory(
                UserManager,
                new OptionsWrapper<IdentityOptions>(UserManager.Options));
            
            authenticationServiceMock = new Mock<IAuthenticationService>();

            permissionService = new PermissionService(DbContext);

            var services = new ServiceCollection();
            services.AddScoped(_ => authenticationServiceMock.Object);
            services.AddSingleton<ProblemDetailsFactory>(new TestProblemDetailsFactory());
            
            var httpContext = new DefaultHttpContext {RequestServices = services.BuildServiceProvider()};

            controller = new AccountController(
                UserManager,
                claimsPrincipalFactory,
                Logger<AccountController>(),
                permissionService)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext }
            };

            user = AddUser(Username, UserType.PCAAdmin, Password);
            user.EmailConfirmed = true;
        }

        [Fact]
        public async Task CanLogin()
        {
            var response = await controller.Login(new AccountController.LoginRequest
            {
                Username = Username,
                Password = Password
            });

            // Should return a dictionary containing the logged-in user's claims
            response.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeOfType<AccountController.ClaimsAndGlobalPermissionsResponse>()
                .Which.Claims.Should().BeOfType<Dictionary<string, string>>()
                .Which.Should().ContainKeys(
                    ClaimTypes.NameIdentifier,
                    AppClaimTypes.UserType);
            
            // Should return an array containing the logged-in user's global permissions
            response.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeOfType<AccountController.ClaimsAndGlobalPermissionsResponse>()
                .Which.GlobalPermissions.Should().BeOfType<GlobalPermission[]>();

            // Should sign-in the user
            authenticationServiceMock.Verify(x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                IdentityConstants.ApplicationScheme,
                It.Is<ClaimsPrincipal>(p => p.Identity.Name == Username),
                It.IsAny<AuthenticationProperties>()
            ));
        }

        [Fact]
        public async Task InvalidCredentialsReturnBadRequest()
        {
            var response = await controller.Login(new AccountController.LoginRequest
            {
                Username = Username,
                Password = "0000"
            });

            response.Should().BeOfType<ObjectResult>()
                .Which.Value.Should().BeOfType<ProblemDetails>()
                .Which.Type.Should().Be("app:account:login:bad-credentials");
            
            // Does not sign-in
            authenticationServiceMock.Verify(
                x => x.SignInAsync(
                    It.IsAny<HttpContext>(), 
                    It.IsAny<string>(), 
                    It.IsAny<ClaimsPrincipal>(), 
                    It.IsAny<AuthenticationProperties>()), 
                Times.Never);
        }

        [Fact]
        public async Task CantLoginIfEmailNotConfirmed()
        {
            user.EmailConfirmed = false;

            var response = await controller.Login(new AccountController.LoginRequest
            {
                Username = Username,
                Password = Password
            });

            response.Should().BeOfType<ObjectResult>()
                .Which.Value.Should().BeOfType<ProblemDetails>()
                .Which.Type.Should().Be("app:account:login:unconfirmed");
            
            // Does not sign-in
            authenticationServiceMock.Verify(
                x => x.SignInAsync(
                    It.IsAny<HttpContext>(), 
                    It.IsAny<string>(), 
                    It.IsAny<ClaimsPrincipal>(), 
                    It.IsAny<AuthenticationProperties>()), 
                Times.Never);
        }

        [Fact]
        public async Task CanLogout()
        {
            await controller.Logout();

            authenticationServiceMock.Verify(
                x => x.SignOutAsync(
                    It.IsAny<HttpContext>(),
                    IdentityConstants.ApplicationScheme,
                    It.IsAny<AuthenticationProperties>()),
                Times.Once);
        }

        [Fact]
        public async Task CanGetCurrentUsersClaims()
        {
            controller.HttpContext.User = await claimsPrincipalFactory.CreateAsync(user);
            
            var response = await controller.Refresh();

            response.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeOfType<AccountController.ClaimsAndGlobalPermissionsResponse>()
                .Which.Claims.Should().BeOfType<Dictionary<string, string>>()
                .Which.Should().ContainKeys(
                    ClaimTypes.NameIdentifier,
                    AppClaimTypes.UserType);
        }
        
        [Fact]
        public async Task CanGetCurrentUsersGlobalPermissions()
        {
            controller.HttpContext.User = await claimsPrincipalFactory.CreateAsync(user);
            
            var response = await controller.Refresh();

            response.Should().BeOfType<OkObjectResult>()
                .Which.Value.Should().BeOfType<AccountController.ClaimsAndGlobalPermissionsResponse>()
                .Which.GlobalPermissions.Should().BeOfType<GlobalPermission[]>();
        }
    }
}
