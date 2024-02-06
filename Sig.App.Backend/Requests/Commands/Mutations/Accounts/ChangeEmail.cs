using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Services.Mailer;
using Sig.App.Backend.Extensions;

namespace Sig.App.Backend.Requests.Commands.Mutations.Accounts
{
    public class ChangeEmail : IRequestHandler<ChangeEmail.Input>
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly UserManager<AppUser> userManager;
        private readonly IMailer mailer;
        private readonly ILogger<ChangeEmail> logger;

        public ChangeEmail(IHttpContextAccessor httpContextAccessor, UserManager<AppUser> userManager, IMailer mailer, ILogger<ChangeEmail> logger)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.userManager = userManager;
            this.mailer = mailer;
            this.logger = logger;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            if (request.NewEmail == "") throw new EmailEmptyException();

            var user = await userManager.FindByIdAsync(httpContextAccessor.HttpContext.User.GetUserId()); 
            var token = await userManager.GenerateChangeEmailTokenAsync(user, request.NewEmail.Trim());

            logger.LogInformation($"User {user.Email} requesting new email {request.NewEmail}. Sending email confirmation.");

            await mailer.Send(new ChangeEmailEmail(request.NewEmail?.Trim(), token, user.Profile?.FirstName));
        }

        [MutationInput]
        public class Input : IRequest
        {
            public string NewEmail { get; set; }
        }

        public abstract class ChangeEmailException : RequestValidationException { }
        public class EmailEmptyException : ChangeEmailException { }
    }
}
