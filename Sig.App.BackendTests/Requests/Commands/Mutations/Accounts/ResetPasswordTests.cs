using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Accounts;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Accounts
{
    public class ResetPasswordTests : TestBase
    {
        private readonly AppUser user;
        private readonly ResetPassword handler;

        public ResetPasswordTests()
        {
            user = AddUser("test@example.com", UserType.PCAAdmin);

            handler = new ResetPassword(UserManager, NullLogger<ResetPassword>.Instance);
        }

        [Fact]
        public async Task ResetsThePassword()
        {
            var input = new ResetPassword.Input {
                EmailAddress = "test@example.com",
                Token = await UserManager.GeneratePasswordResetTokenAsync(user),
                NewPassword = "1234aAuuuuuu!"
            };

            await handler.Handle(input, CancellationToken.None);

            (await UserManager.CheckPasswordAsync(user, "1234aAuuuuuu!")).Should().BeTrue();
        }

        [Fact]
        public async Task ThrowsIfUnknownUser()
        {
            var input = new ResetPassword.Input {
                EmailAddress = "unknown@example.com",
                Token = await UserManager.GeneratePasswordResetTokenAsync(user),
                NewPassword = "1234aAuuuuuu!"
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<IdentityResultException>();
        }

        [Fact]
        public async Task ThrowsIfInvalidToken()
        {
            var input = new ResetPassword.Input {
                EmailAddress = "test@example.com",
                Token = "bad-token",
                NewPassword = "1234aAuuuuuu!"
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<IdentityResultException>();
        }

        [Fact]
        public async Task ThrowsIfInvalidPassword()
        {
            var input = new ResetPassword.Input
            {
                EmailAddress = "test@example.com",
                Token = await UserManager.GeneratePasswordResetTokenAsync(user),
                NewPassword = "password"
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<IdentityResultException>();
        }
    }
}