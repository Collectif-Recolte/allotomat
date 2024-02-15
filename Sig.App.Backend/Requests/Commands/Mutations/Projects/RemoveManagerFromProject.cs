using GraphQL.Conventions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Projects
{
    public class RemoveManagerFromProject : IRequestHandler<RemoveManagerFromProject.Input, RemoveManagerFromProject.Payload>
    {
        private readonly ILogger<RemoveManagerFromProject> logger;
        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;

        public RemoveManagerFromProject(ILogger<RemoveManagerFromProject> logger, AppDbContext db, UserManager<AppUser> userManager)
        {
            this.logger = logger;
            this.db = db;
            this.userManager = userManager;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] RemoveManagerFromProject({request.ManagerId})");
            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var project = await db.Projects.FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

            if (project == null)
            {
                logger.LogWarning("[Mutation] RemoveManagerFromProject - ProjectNotFoundException");
                throw new ProjectNotFoundException();
            }
            
            var manager = await db.Users.FirstOrDefaultAsync(x => x.Id == request.ManagerId.IdentifierForType<AppUser>());

            if (manager == null)
            {
                logger.LogWarning("[Mutation] RemoveManagerFromProject - ManagerNotFoundException");
                throw new ManagerNotFoundException();
            }

            await userManager.RemoveClaimAsync(manager, new Claim(AppClaimTypes.ProjectManagerOf, project.Id.ToString()));

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"Project manager {manager.Email} remove from project {project.Name} ({project.Id})");

            return new Payload
            {
                Project = new ProjectGraphType(project)
            };
        }

        public class ProjectNotFoundException : RequestValidationException { }
        public class ManagerNotFoundException : RequestValidationException { }


        [MutationInput]
        public class Input : HaveProjectId, IRequest<Payload>
        {
            public Id ManagerId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public ProjectGraphType Project { get; set; }
        }
    }
}