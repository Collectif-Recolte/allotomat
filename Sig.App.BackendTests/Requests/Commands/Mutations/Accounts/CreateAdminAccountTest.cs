using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Requests.Commands.Mutations.Accounts;
using Sig.App.Backend.Services.Mailer;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Accounts
{
    public class CreateAdminAccountTest : TestBase
    {
        private readonly CreateAdminAccount handler;
        private readonly Mock<IMailer> mailer;

        public CreateAdminAccountTest()
        {
            mailer = new Mock<IMailer>();
            handler = new CreateAdminAccount(UserManager, mailer.Object, NullLogger<CreateAdminAccount>.Instance);
        }

        [Fact]
        public async Task CreatesTheAccount()
        {
            var input = new CreateAdminAccount.Input
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "test@example.com"
            };

            await handler.Handle(input, CancellationToken.None);

            var user = await DbContext.Users.Include(x => x.Profile).FirstAsync();

            user.Profile.FirstName.Should().Be("John");
            user.Profile.LastName.Should().Be("Doe");
            user.Email.Should().Be("test@example.com");
            user.Type.Should().Be(UserType.PCAAdmin);
            user.EmailConfirmed.Should().BeFalse();
        }

        [Fact]
        public async Task SendsConfirmationEmail()
        {
            var input = new CreateAdminAccount.Input
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "test@example.com"
            };

            await handler.Handle(input, CancellationToken.None);

            mailer.Verify(x => x.Send(It.IsAny<AdminInviteEmail>()));
        }
    }
}
