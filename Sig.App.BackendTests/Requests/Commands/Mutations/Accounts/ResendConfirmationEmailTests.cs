using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Requests.Commands.Mutations.Accounts;
using Sig.App.Backend.Services.Mailer;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Accounts
{
    public class ResendConfirmationEmailTests : TestBase
    {
        private readonly AppUser user;
        private readonly Mock<IMailer> mailer;
        private readonly IRequestHandler<ResendConfirmationEmail.Input> handler;

        public ResendConfirmationEmailTests()
        {
            user = AddUser("test@example.com", UserType.PCAAdmin);
            user.EmailConfirmed = false;
            user.Profile = new UserProfile { FirstName = "Test" };

            mailer = new Mock<IMailer>();
            handler = new ResendConfirmationEmail(DbContext, UserManager, mailer.Object, NullLogger<ResendConfirmationEmail>.Instance);
        }

        [Fact]
        public async Task ResendsTheConfirmationEmail()
        {
            var input = new ResendConfirmationEmail.Input
            {
                Email = "test@example.com"
            };

            await handler.Handle(input, CancellationToken.None);

            mailer.Verify(x => x.Send(It.IsAny<AdminInviteEmail>()));
        }

        [Fact]
        public async Task ThrowsIfEmailAlreadyConfirmed()
        {
            user.EmailConfirmed = true;
            await DbContext.SaveChangesAsync();

            var input = new ResendConfirmationEmail.Input
            {
                Email = "test@example.com"
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ResendConfirmationEmail.NoNeedToConfirmException>();
        }
    }
}
