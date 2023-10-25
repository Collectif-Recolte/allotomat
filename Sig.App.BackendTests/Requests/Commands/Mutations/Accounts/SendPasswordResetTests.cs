using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Accounts;
using Sig.App.Backend.Services.Mailer;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Accounts
{
    public class SendPasswordResetTests : TestBase
    {
        private readonly AppUser user;
        private readonly Mock<IMailer> mailer;
        private readonly IRequestHandler<SendPasswordReset.Input> handler;

        public SendPasswordResetTests()
        {
            user = AddUser("test@example.com", UserType.PCAAdmin);

            mailer = new Mock<IMailer>();
            handler = new SendPasswordReset(UserManager, mailer.Object, NullLogger<SendPasswordReset>.Instance, Mediator);
        }
        
        [Fact]
        public async Task SendsResetEmailWithValidToken()
        {
            var input = new SendPasswordReset.Input {
                Email = "test@example.com"
            };

            await handler.Handle(input, CancellationToken.None);

            mailer.Verify(x => x.Send(It.Is<ResetPasswordEmail>(email => email.To.Contains("test@example.com"))));

            var token = mailer.Invocations[0].Arguments[0].As<ResetPasswordEmail>().Token;
            (await UserManager.ResetPasswordAsync(user, token, "1234aAuuuuuu!")).AssertSuccess();
        }

        [Fact]
        public async Task DoesNothingIfUnknownEmail()
        {
            var input = new SendPasswordReset.Input {
                Email = "unknown@example.com"
            };

            await handler.Handle(input, CancellationToken.None);

            mailer.Verify(x => x.Send(It.IsAny<EmailModel>()), Times.Never);
        }
    }
}