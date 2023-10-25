using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Accounts;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Accounts
{
    public class ConfirmChangeEmailTests : TestBase
    {
        private readonly AppUser user;
        private readonly ConfirmChangeEmail handler;

        public ConfirmChangeEmailTests()
        {
            user = AddUser("test@example.com", UserType.PCAAdmin);
            SetLoggedInUser(user);

            handler = new ConfirmChangeEmail(UserManager, HttpContextAccessor, NullLogger<ConfirmChangeEmail>.Instance);
        }

        [Fact]
        public async Task ChangesTheEmailOfTheCurrentUser()
        {
            var validToken = await UserManager.GenerateChangeEmailTokenAsync(user, "new@example.com");
            var input = new ConfirmChangeEmail.Input
            {
                NewEmail = "new@example.com",
                Token = validToken
            };

            await handler.Handle(input, CancellationToken.None);

            user.Email.Should().Be("new@example.com");
            user.EmailConfirmed.Should().BeTrue();
        }

        [Fact]
        public async Task FailsWithBadToken()
        {
            var invalidToken = await UserManager.GenerateChangeEmailTokenAsync(user, "new@example.com");
            var input = new ConfirmChangeEmail.Input
            {
                NewEmail = "other@example.com", // Does not match the token
                Token = invalidToken
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<IdentityResultException>();
        }
    }
}
