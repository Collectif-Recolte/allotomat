using Sig.App.Backend.Extensions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Services.Mailer;

namespace Sig.App.Backend.Requests.Commands.Mutations.Accounts
{
    public class CreateAdminAccount : IRequestHandler<CreateAdminAccount.Input, CreateAdminAccount.Payload>
    {
        private readonly UserManager<AppUser> userManager;
        private readonly IMailer mailer;
        private readonly ILogger<CreateAdminAccount> logger;

        public CreateAdminAccount(UserManager<AppUser> userManager, IMailer mailer, ILogger<CreateAdminAccount> logger)
        {
            this.userManager = userManager;
            this.mailer = mailer;
            this.logger = logger;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] CreateAdminAccount({request.FirstName}, {request.LastName}, {request.Email})");
            var user = new AppUser(request.Email?.Trim())
            {
                Type = UserType.PCAAdmin,
                Profile = new UserProfile
                {
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim()
                }
            };

            var result = await userManager.CreateAsync(user);
            result.AssertSuccess();

            var token = await userManager.GenerateUserTokenAsync(user, TokenProviders.EmailInvites, TokenPurposes.AdminInvite);
            await mailer.Send(new AdminInviteEmail(request.Email, token, request.FirstName));

            logger.LogInformation($"[Mutation] CreateAdminAccount - Admin account created ({user.Email}).");

            return new Payload
            {
                User = new UserGraphType(user)
            };
        }

        [MutationInput]
        public class Input : IRequest<Payload>
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public UserGraphType User { get; set; }
        }
    }
}
