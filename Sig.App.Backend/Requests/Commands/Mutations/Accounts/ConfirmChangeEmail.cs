using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Extensions;

namespace Sig.App.Backend.Requests.Commands.Mutations.Accounts
{
    public class ConfirmChangeEmail : IRequestHandler<ConfirmChangeEmail.Input, ConfirmChangeEmail.Payload>
    {
        private readonly UserManager<AppUser> userManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<ConfirmChangeEmail> logger;

        public ConfirmChangeEmail(UserManager<AppUser> userManager, IHttpContextAccessor httpContextAccessor, ILogger<ConfirmChangeEmail> logger)
        {
            this.userManager = userManager;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] ConfirmChangeEmail({request.NewEmail})");
            var userId = httpContextAccessor.HttpContext.User.GetUserId();
            var user = await userManager.FindByIdAsync(userId);
            var previousEmail = user.Email;

            var result = await userManager.ChangeEmailAsync(user, request.NewEmail, request.Token);
            result.AssertSuccess();

            logger.LogInformation($"[Mutation] ConfirmChangeEmail - Email change confirmed from {previousEmail} to {request.NewEmail}");

            return new Payload
            {
                User = new UserGraphType(user)
            };
        }

        [MutationInput]
        public class Input : IRequest<Payload>
        {
            public string NewEmail { get; set; }
            public string Token { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public UserGraphType User { get; set; }
        }
    }
}
