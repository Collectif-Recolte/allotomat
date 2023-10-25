using GraphQL.Conventions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Services.Mailer;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Projects
{
    public class AddManagerToProject : IRequestHandler<AddManagerToProject.Input, AddManagerToProject.Payload>
    {
        private readonly ILogger<AddManagerToProject> logger;
        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;
        private readonly IMailer mailer;

        public AddManagerToProject(ILogger<AddManagerToProject> logger, AppDbContext db, UserManager<AppUser> userManager, IMailer mailer)
        {
            this.logger = logger;
            this.db = db;
            this.userManager = userManager;
            this.mailer = mailer;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var project = await db.Projects.FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

            if (project == null) throw new ProjectNotFoundException();
            var managers = new List<AppUser>();

            foreach (var email in request.ManagerEmails)
            {
                var (manager, isNew) = await GetOrCreateProjectManager(email);
                var existingClaims = await userManager.GetClaimsAsync(manager);
                if (existingClaims.Any(c => c.Type == AppClaimTypes.ProjectManagerOf))
                    throw new UserAlreadyManagerException();

                await userManager.AddClaimAsync(manager, new Claim(AppClaimTypes.ProjectManagerOf, project.Id.ToString()));

                if (isNew)
                {
                    await mailer.Send(new ProjectManagerInviteEmail(manager.Email)
                    {
                        InviteToken = await userManager.GenerateUserTokenAsync(manager, TokenProviders.EmailInvites, TokenPurposes.ProjectManagerInvite),
                        ProjectName = project.Name
                    });
                }
                else
                {
                    await mailer.Send(new NewProjectAssignedEmail(manager.Email, project.GetIdentifier().ToString(), project.Name));
                }

                managers.Add(manager);
                logger.LogInformation($"Project manager {manager.Email} added to project {project.Name} ({project.Id})");
            }

            await db.SaveChangesAsync(cancellationToken);

            return new Payload
            {
                Managers = managers.Select(x => new UserGraphType(x))
            };
        }

        private async Task<(AppUser user, bool isNew)> GetOrCreateProjectManager(string email)
        {
            var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user != null)
            {
                switch (user.Type)
                {
                    case UserType.ProjectManager:
                        return (user, false);
                    default:
                        throw new ExistingUserNotProjectManagerException();
                }
            }
            else
            {
                user = new AppUser(email)
                {
                    Type = UserType.ProjectManager,
                    Profile = new UserProfile()
                };

                var result = await userManager.CreateAsync(user);
                result.AssertSuccess();

                logger.LogDebug($"New project manager created {user.Email} ({user.Id}). Sending email invitation.");
            }

            return (user, true);
        }

        public class ProjectNotFoundException : RequestValidationException { }
        public class UserAlreadyManagerException : RequestValidationException { }
        public class ExistingUserNotProjectManagerException : RequestValidationException { }

        [MutationInput]
        public class Input : IRequest<Payload>, IHaveProjectId
        {
            public Id ProjectId { get; set; }
            public IEnumerable<string> ManagerEmails { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public IEnumerable<UserGraphType> Managers { get; set; }
        }
    }
}