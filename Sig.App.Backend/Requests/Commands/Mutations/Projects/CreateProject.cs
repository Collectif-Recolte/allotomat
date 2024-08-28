using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Plugins.GraphQL;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Collections.Generic;
using Sig.App.Backend.Services.Mailer;
using Microsoft.AspNetCore.Identity;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Plugins.MediatR;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using System.Linq;
using Sig.App.Backend.Constants;
using System.Security.Claims;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Entities.ProductGroups;

namespace Sig.App.Backend.Requests.Commands.Mutations.Projects
{
    public class CreateProject : IRequestHandler<CreateProject.Input, CreateProject.Payload>
    {
        private readonly ILogger<CreateProject> logger;
        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;
        private readonly IMailer mailer;

        public CreateProject(ILogger<CreateProject> logger, AppDbContext db, UserManager<AppUser> userManager, IMailer mailer)
        {
            this.logger = logger;
            this.db = db;
            this.userManager = userManager;
            this.mailer = mailer;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] CreateProject({request.Name}, {request.Url}, {request.ManagerEmails}, {request.AllowOrganizationsAssignCards}, {request.BeneficiariesAreAnonymous}, {request.AdministrationSubscriptionsOffPlatform})");
            var userAlreadyManagerException = false;
            var existingUserNotProjectManager = false;

            var project = new Project() {
                Name = request.Name.Trim(),
                Url = request.Url,
                AllowOrganizationsAssignCards = request.AllowOrganizationsAssignCards,
                BeneficiariesAreAnonymous = request.BeneficiariesAreAnonymous,
                AdministrationSubscriptionsOffPlatform = request.AdministrationSubscriptionsOffPlatform
            };
            var managers = new List<AppUser>();

            db.Projects.Add(project);
            await db.SaveChangesAsync(cancellationToken);

            foreach (var email in request.ManagerEmails)
            {
                var (manager, isNew, existingUserNotPM) = await GetOrCreateProjectManager(email);
                if (existingUserNotPM)
                {
                    existingUserNotProjectManager = true;
                    continue;
                }
                var existingClaims = await userManager.GetClaimsAsync(manager);
                if (existingClaims.Any(c => c.Type == AppClaimTypes.ProjectManagerOf))
                {
                    userAlreadyManagerException = true;
                    continue;
                }

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
                logger.LogInformation($"[Mutation] CreateProject - Project manager {manager.Email} added to project {project.Name} ({project.Id})");
            }

            if (!request.AdministrationSubscriptionsOffPlatform)
            {
                var defaultProductGroup = new ProductGroup()
                {
                    Name = "Tout/All",
                    Color = ProductGroupColor.Color_1,
                    OrderOfAppearance = 1,
                    Project = project
                };
                db.ProductGroups.Add(defaultProductGroup);
            }
            else
            {
                var defaultProductGroup1 = new ProductGroup()
                {
                    Name = "Fruits & légumes",
                    Color = ProductGroupColor.Color_1,
                    OrderOfAppearance = 1,
                    Project = project
                };
                db.ProductGroups.Add(defaultProductGroup1);
                var defaultProductGroup2 = new ProductGroup()
                {
                    Name = "Épicerie",
                    Color = ProductGroupColor.Color_2,
                    OrderOfAppearance = 2,
                    Project = project
                };
                db.ProductGroups.Add(defaultProductGroup2);
            }

            var productGroupLoyalty = new ProductGroup()
            {
                Name = ProductGroupType.LOYALTY,
                Color = ProductGroupColor.Color_0,
                OrderOfAppearance = -1,
                Project = project
            };
            db.ProductGroups.Add(productGroupLoyalty);

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] CreateProject - New project created {project.Name} ({project.Id})");

            if (userAlreadyManagerException)
            {
                logger.LogWarning("[Mutation] CreateProject - UserAlreadyManagerException");
                throw new UserAlreadyManagerException();
            }

            if (existingUserNotProjectManager)
            {
                logger.LogWarning("[Mutation] CreateProject - ExistingUserNotProjectManagerException");
                throw new ExistingUserNotProjectManagerException();
            }

            return new Payload
            {
                Project = new ProjectGraphType(project),
                Managers = managers.Select(x => new UserGraphType(x))
            };
        }

        private async Task<(AppUser user, bool isNew, bool existingUserNotPM)> GetOrCreateProjectManager(string email)
        {
            var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);

            if (user != null)
            {
                switch (user.Type)
                {
                    case UserType.ProjectManager:
                        return (user, false, false);
                    default:
                        return (user, false, true);
                }
            }
            else
            {
                user = new AppUser(email)
                {
                    Type = UserType.ProjectManager,
                    Profile = new UserProfile(),
                };

                var result = await userManager.CreateAsync(user);
                result.AssertSuccess();

                logger.LogInformation($"[Mutation] CreateProject - New project manager created {user.Email} ({user.Id}). Sending email invitation.");
            }

            return (user, true, false);
        }

        [MutationInput]
        public class Input : IRequest<Payload>
        {
            public string Name { get; set; }
            public string Url { get; set; }
            public IEnumerable<string> ManagerEmails { get; set; }
            public bool AllowOrganizationsAssignCards { get; set; }
            public bool BeneficiariesAreAnonymous { get; set; }
            public bool AdministrationSubscriptionsOffPlatform { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public ProjectGraphType Project { get; set; }
            public IEnumerable<UserGraphType> Managers { get; set; }
        }

        public class UserAlreadyManagerException : RequestValidationException { }
        public class ExistingUserNotProjectManagerException : RequestValidationException { }
    }
}
