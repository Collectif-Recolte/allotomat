using GraphQL.Conventions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;

namespace Sig.App.Backend.Requests.Commands.Mutations.Accounts
{
    public class ResetPassword : IRequestHandler<ResetPassword.Input, ResetPassword.Payload>
    {
        private readonly UserManager<AppUser> userManager;
        private readonly ILogger<ResetPassword> logger;

        public ResetPassword(UserManager<AppUser> userManager, ILogger<ResetPassword> logger)
        {
            this.userManager = userManager;
            this.logger = logger;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            var user = await userManager.FindByEmailAsync(request.EmailAddress);

            // Fail with InvalidToken error to not leak info about email validity
            if (user == null)
                throw new IdentityResultException(IdentityResult.Failed(userManager.ErrorDescriber.InvalidToken()));

            var result = await userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
            result.AssertSuccess();

            logger.LogInformation($"Password reset successful for user {user.Email}");

            return new Payload
            {
                User = new UserGraphType(user)
            };
        }

        [InputType, MutationInput]
        public class Input : IRequest<Payload>
        {
            public NonNull<string> EmailAddress { get; set; }
            public NonNull<string> Token { get; set; }
            public NonNull<string> NewPassword { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public UserGraphType User { get; set; }
        }
    }
}
