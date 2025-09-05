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
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Services.Mailer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Organizations
{
    public class CreateOrganizationInProject : IRequestHandler<CreateOrganizationInProject.Input, CreateOrganizationInProject.Payload>
    {
        private readonly ILogger<CreateOrganizationInProject> logger;
        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;
        private readonly IMailer mailer;

        public CreateOrganizationInProject(ILogger<CreateOrganizationInProject> logger, AppDbContext db, UserManager<AppUser> userManager, IMailer mailer)
        {
            this.logger = logger;
            this.db = db;
            this.userManager = userManager;
            this.mailer = mailer;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] CreateOrganizationInProject({request.ProjectId}, {request.Name}, {request.ManagerEmails})");
            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var project = await db.Projects.FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

            if (project == null)
            {
                logger.LogWarning("[Mutation] CreateOrganizationInProject - ProjectNotFoundException");
                throw new ProjectNotFoundException();
            }

            var organization = new Organization()
            {
                Name = request.Name.Trim(),
                Project = project
            };
            
            var managers = new List<AppUser>();
            
            db.Organizations.Add(organization);

            foreach (var email in request.ManagerEmails)
            {
                var (manager, isNew) = await GetOrCreateOrganizationManager(email);
                var existingClaims = await userManager.GetClaimsAsync(manager);
                if (existingClaims.Any(c => c.Type == AppClaimTypes.OrganizationManagerOf))
                {
                    logger.LogWarning($"[Mutation] CreateOrganizationInProject - UserAlreadyManagerException ({email})");
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
                logger.LogInformation($"[Mutation] CreateOrganizationInProject - Organization manager {manager.Email} added to Organization {organization.Name} ({organization.Id})");
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] CreateOrganizationInProject - New organization created {organization.Name} ({organization.Id})");

            return new Payload
            {
                Organization = new OrganizationGraphType(organization)
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
                        logger.LogWarning($"[Mutation] CreateOrganizationInProject - ExistingUserNotOrganizationManagerException ({email})");
                        throw new ExistingUserNotOrganizationManagerException();
                }
            }
            else
            {
                user = new AppUser(email)
                {
                    Type = UserType.OrganizationManager,
                    Profile = new UserProfile(),
                    EmailOptIn = new UserEmailOptIn()
                };

                var result = await userManager.CreateAsync(user);
                result.AssertSuccess();

                logger.LogInformation($"[Mutation] CreateOrganizationInProject - New organization manager created {user.Email} ({user.Id}). Sending email invitation.");
            }

            return (user, true);
        }

        [MutationInput]
        public class Input : HaveProjectId, IRequest<Payload>
        {
            public string Name { get; set; }
            public IEnumerable<string> ManagerEmails { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public OrganizationGraphType Organization { get; set; }
        }

        public class ProjectNotFoundException : RequestValidationException { }
        public class UserAlreadyManagerException : RequestValidationException { }
        public class ExistingUserNotOrganizationManagerException : RequestValidationException { }
    }
}
