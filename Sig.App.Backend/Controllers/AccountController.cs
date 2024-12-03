using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Services.Permission;
using Sig.App.Backend.Services.Permission.Enums;
using Sig.App.Backend.DbModel.Enums;

namespace Sig.App.Backend.Controllers
{
    [Route("account")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> userManager;
        private readonly IUserClaimsPrincipalFactory<AppUser> claimsPrincipalFactory;
        private readonly ILogger<AccountController> logger;
        private readonly PermissionService permissionService;

        public AccountController(
            UserManager<AppUser> userManager,
            IUserClaimsPrincipalFactory<AppUser> claimsPrincipalFactory,
            ILogger<AccountController> logger,
            PermissionService permissionService)
        {
            this.userManager = userManager;
            this.claimsPrincipalFactory = claimsPrincipalFactory;
            this.logger = logger;
            this.permissionService = permissionService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await userManager.FindByNameAsync(request.Username);
            var valid = user != null && await userManager.CheckPasswordAsync(user, request.Password);

            if (!valid)
            {
                logger.LogWarning($"Failed logon for {request.Username}");
                return Problem(
                    type: "app:account:login:bad-credentials",
                    title: "Wrong username or password", 
                    statusCode: 400);
            }

            if (!user.EmailConfirmed)
            {
                logger.LogWarning($"Login rejected for {request.Username} (email not confirmed)");
                return Problem(
                    type: "app:account:login:unconfirmed",
                    title: "Email not confirmed",
                    statusCode: 400);
            }

            user.LastAccessTimeUtc = DateTime.UtcNow;
            user.State = UserState.Active;
            await userManager.UpdateAsync(user);

            var principal = await claimsPrincipalFactory.CreateAsync(user);
            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);
            
            var response = new ClaimsAndGlobalPermissionsResponse()
            {
                Claims = principal.Claims.ToDictionary(x => x.Type, x => x.Value),
                GlobalPermissions = await permissionService.GetGlobalPermissions(principal)
            };

            return Ok(response);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            return NoContent();
        }

        [HttpGet("refresh"), Authorize]
        public async Task<IActionResult> Refresh()
        {
            var response = new ClaimsAndGlobalPermissionsResponse()
            {
                Claims = User.Claims.ToDictionary(x => x.Type, x => x.Value),
                GlobalPermissions = await permissionService.GetGlobalPermissions(User)
            };
            
            return Ok(response);
        }

        public class LoginRequest
        {
            [Required]
            public string Username { get; set; }
            [Required]
            public string Password { get; set; }
        }

        public class ClaimsAndGlobalPermissionsResponse
        {
            public Dictionary<string, string> Claims { get; set; }
            public GlobalPermission[] GlobalPermissions { get; set; }
        }
    }
}
