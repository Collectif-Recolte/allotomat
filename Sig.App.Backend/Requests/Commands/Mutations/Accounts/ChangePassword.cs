using GraphQL.Conventions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Extensions;

namespace Sig.App.Backend.Requests.Commands.Mutations.Accounts
{
    public class ChangePassword : IRequestHandler<ChangePassword.Input, ChangePassword.Payload>
    {
        private readonly UserManager<AppUser> userManager;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<ChangePassword> logger;

        public ChangePassword(UserManager<AppUser> userManager, IHttpContextAccessor httpContextAccessor, ILogger<ChangePassword> logger)
        {
            this.userManager = userManager;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByIdAsync(httpContextAccessor.HttpContext.User.GetUserId());

            if (!await userManager.CheckPasswordAsync(user, request.CurrentPassword))
            {
                logger.LogWarning("[Mutation] ChangePassword - WrongPasswordException");
                throw new WrongPasswordException();
            }

            var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            result.AssertSuccess();

            logger.LogInformation($"Password changed for user {user.Email}");

            return new Payload
            {
                User = new UserGraphType(user)
            };
        }

        [MutationInput]
        public class Input : IRequest<Payload>
        {
            public NonNull<string> CurrentPassword { get; set; }
            public NonNull<string> NewPassword { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public UserGraphType User { get; set; }
        }

        public abstract class ChangePasswordException : RequestValidationException { }
        public class WrongPasswordException : ChangePasswordException { }
    }
}
