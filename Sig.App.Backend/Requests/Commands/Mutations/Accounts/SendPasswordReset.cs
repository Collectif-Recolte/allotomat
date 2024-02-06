using GraphQL.Conventions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Services.Mailer;

namespace Sig.App.Backend.Requests.Commands.Mutations.Accounts
{
    public class SendPasswordReset : IRequestHandler<SendPasswordReset.Input>
    {
        private readonly UserManager<AppUser> userManager;
        private readonly IMailer mailer;
        private readonly ILogger<SendPasswordReset> logger;
        private readonly IMediator mediator;

        public SendPasswordReset(UserManager<AppUser> userManager, IMailer mailer, ILogger<SendPasswordReset> logger, IMediator mediator)
        {
            this.userManager = userManager;
            this.mailer = mailer;
            this.logger = logger;
            this.mediator = mediator;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null) return;

            if (!user.EmailConfirmed)
            {
                await mediator.Send(new ResendConfirmationEmail.Input
                {
                    Email = request.Email
                });

                return;
            }

            logger.LogInformation($"Password reset requested for user {user.Email}");

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            await mailer.Send(new ResetPasswordEmail(request.Email)
            {
                UserName = user.UserName,
                Token = token
            });
        }

        [MutationInput]
        public class Input : IRequest
        {
            public NonNull<string> Email { get; set; }
        }
    }
}
