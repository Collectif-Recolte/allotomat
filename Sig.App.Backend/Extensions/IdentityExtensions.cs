using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Threading.Tasks;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Enums;

namespace Sig.App.Backend.Extensions
{
    public static class IdentityExtensions
    {
        public static void AssertSuccess(this IdentityResult result)
        {
            if (result.Succeeded) return;
            throw new IdentityResultException(result);
        }

        public static async Task<IdentityResult> CreateOrUpdateAsync(
            this UserManager<AppUser> userManager, 
            AppDbContext db, 
            string email, string firstName, string lastName, UserType type, 
            string password, bool allowUpdatePassword = true)
        {
            var existingUser = await userManager.FindByEmailAsync(email);
            if (existingUser == null)
            {
                existingUser = new AppUser(email) { Type = type, EmailConfirmed = true, Profile = new UserProfile() };

                existingUser.Profile.FirstName = firstName;
                existingUser.Profile.LastName = lastName;

                return await userManager.CreateAsync(existingUser, password);
            }

            existingUser.Type = type;
            existingUser.EmailConfirmed = true;

            var result = await userManager.UpdateAsync(existingUser);

            if (result.Succeeded && allowUpdatePassword && !await userManager.CheckPasswordAsync(existingUser, password))
            {
                await userManager.RemovePasswordAsync(existingUser);
                result = await userManager.AddPasswordAsync(existingUser, password);
            }

            var profile = await db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == existingUser.Id);
            if (profile == null)
            {
                profile = new UserProfile();
                profile.UserId = existingUser.Id;
                db.UserProfiles.Add(profile);
            }

                profile.FirstName = firstName;
                profile.LastName = lastName;

                await db.SaveChangesAsync();

                return result;
            
        }

        public static string GetUserId(this ClaimsPrincipal principal)
        {
            return (principal.Identity as ClaimsIdentity)?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public static bool IsUserType(this ClaimsPrincipal principal, UserType type)
        {
            return principal.HasClaim(AppClaimTypes.UserType, type.ToString());
        }

        public static bool IsAdmin(this ClaimsPrincipal principal) => principal.IsUserType(UserType.PCAAdmin);

        public static bool IsExpected(this IdentityResultException exception)
        {
            var hasUnexpectedErrorCode = exception.IdentityResult.Errors.Any(
                e => e.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure) ||
                     e.Code == nameof(IdentityErrorDescriber.DefaultError) ||
                     e.Code == nameof(IdentityErrorDescriber.UserAlreadyHasPassword)
            );

            return !hasUnexpectedErrorCode;
        }
    }

    [Serializable]
    public class IdentityResultException : Exception
    {
        public IdentityResult IdentityResult { get; }

        public override string Message =>
            string.Join('\n', IdentityResult.Errors.Select(err => $"{err.Code}: {err.Description}"));

        public override IDictionary Data => IdentityResult.Errors.ToDictionary(x => x.Code, x => x.Description);

        public IdentityResultException(IdentityResult result)
        {
            IdentityResult = result;
        }

        public IdentityResultException(string message) : base(message)
        {
        }

        public IdentityResultException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}