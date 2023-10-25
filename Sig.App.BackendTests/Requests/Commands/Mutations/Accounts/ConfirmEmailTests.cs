using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Requests.Commands.Mutations.Accounts;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Accounts
{
    public class ConfirmEmailTests : TestBase
    {
        private readonly AppUser user;
        private readonly IRequestHandler<ConfirmEmail.Input> handler;

        public ConfirmEmailTests()
        {
            user = AddUser("test@example.com", UserType.PCAAdmin);
            user.EmailConfirmed = false;

            handler = new ConfirmEmail(UserManager, NullLogger<ConfirmEmail>.Instance);
        }

        [Fact]
        public async Task MarksEmailAsConfirmed()
        {
            var input = new ConfirmEmail.Input
            {
                Email = "test@example.com",
                Token = await UserManager.GenerateEmailConfirmationTokenAsync(user)
            };

            await handler.Handle(input, CancellationToken.None);

            user.EmailConfirmed.Should().BeTrue();
        }
    }
}
