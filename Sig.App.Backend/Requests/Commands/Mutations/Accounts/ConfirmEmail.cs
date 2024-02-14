using Sig.App.Backend.Extensions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;

namespace Sig.App.Backend.Requests.Commands.Mutations.Accounts
{
    public class ConfirmEmail : IRequestHandler<ConfirmEmail.Input>
    {
        private readonly UserManager<AppUser> userManager;
        private readonly ILogger<ConfirmEmail> logger;

        public ConfirmEmail(UserManager<AppUser> userManager, ILogger<ConfirmEmail> logger)
        {
            this.userManager = userManager;
            this.logger = logger;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] ConfirmEmail({request.Email})");
            var user = await userManager.FindByEmailAsync(request.Email);
            if (user == null || user.EmailConfirmed)
            {
                logger.LogWarning("[Mutation] ConfirmEmail - NoNeedToConfirmException");
                throw new NoNeedToConfirmException();
            }

            var result = await userManager.ConfirmEmailAsync(user, request.Token);
            result.AssertSuccess();

            logger.LogInformation($"Email address confirmed {user.Email}");
        }

        [MutationInput]
        public class Input : IRequest
        {
            public string Email { get; set; }
            public string Token { get; set; }
        }

        public abstract class ConfirmEmailException : RequestValidationException { }
        public class NoNeedToConfirmException : ConfirmEmailException { }
    }
}
