using GraphQL.Conventions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Services.Mailer;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Organizations
{
    public class AddManagerToOrganization : IRequestHandler<AddManagerToOrganization.Input, AddManagerToOrganization.Payload>
    {
        private readonly ILogger<AddManagerToOrganization> logger;
        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;
        private readonly IMailer mailer;

        public AddManagerToOrganization(ILogger<AddManagerToOrganization> logger, AppDbContext db, UserManager<AppUser> userManager, IMailer mailer)
        {
            this.logger = logger;
            this.db = db;
            this.userManager = userManager;
            this.mailer = mailer;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AddManagerToOrganization({request.OrganizationId}, {request.ManagerEmails})");
            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations.FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null)
            {
                logger.LogWarning("[Mutation] AddManagerToOrganization - OrganizationNotFoundException");
                throw new OrganizationNotFoundException();
            }
            var managers = new List<AppUser>();

            foreach (var email in request.ManagerEmails)
            {
                var (manager, isNew) = await GetOrCreateOrganizationManager(email);
                var existingClaims = await userManager.GetClaimsAsync(manager);
                if (existingClaims.Any(c => c.Type == AppClaimTypes.OrganizationManagerOf))
                {
                    logger.LogWarning("[Mutation] AddManagerToOrganization - UserAlreadyManagerException");
                    throw new UserAlreadyManagerException();
                }

                await userManager.AddClaimAsync(manager, new Claim(AppClaimTypes.OrganizationManagerOf, organization.Id.ToString()));

                if (isNew)
                {
                    await mailer.Send(new OrganizationManagerInviteEmail(manager.Email)
                    {
                        InviteToken = await userManager.GenerateUserTokenAsync(manager, TokenProviders.EmailInvites, TokenPurposes.OrganizationManagerInvite),
                        OrganizationName = organization.Name
                    });
                }
                else
                {
                    await mailer.Send(new NewOrganizationAssignedEmail(manager.Email, organization.GetIdentifier().ToString(), organization.Name));
                }

                managers.Add(manager);
                logger.LogInformation($"[Mutation] AddManagerToOrganization - Organization manager {manager.Email} added to organization {organization.Name} ({organization.Id})");
            }

            await db.SaveChangesAsync(cancellationToken);

            return new Payload
            {
                Managers = managers.Select(x => new UserGraphType(x))
            };
        }

        private async Task<(AppUser user, bool isNew)> GetOrCreateOrganizationManager(string email)
        {
            var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user != null)
            {
                switch (user.Type)
                {
                    case UserType.OrganizationManager:
                        return (user, false);
                    default:
                        logger.LogWarning($"[Mutation] AddManagerToOrganization - ExistingUserNotOrganizationManagerException ({email})");
                        throw new ExistingUserNotOrganizationManagerException();
                }
            }
            else
            {
                user = new AppUser(email)
                {
                    Type = UserType.OrganizationManager,
                    Profile = new UserProfile()
                };

                var result = await userManager.CreateAsync(user);
                result.AssertSuccess();

                logger.LogInformation($"[Mutation] AddManagerToOrganization - New Organization manager created {user.Email} ({user.Id}). Sending email invitation.");
            }

            return (user, true);
        }

        public class OrganizationNotFoundException : RequestValidationException { }
        public class UserAlreadyManagerException : RequestValidationException { }
        public class ExistingUserNotOrganizationManagerException : RequestValidationException { }

        [MutationInput]
        public class Input : HaveOrganizationId, IRequest<Payload>
        {
            public IEnumerable<string> ManagerEmails { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public IEnumerable<UserGraphType> Managers { get; set; }
        }
    }
}