using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Projects
{
    public class EditProject : IRequestHandler<EditProject.Input, EditProject.Payload>
    {
        private readonly ILogger<EditProject> logger;
        private readonly AppDbContext db;

        public EditProject(ILogger<EditProject> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] EditProject({request.Name}, {request.Url}, {request.AllowOrganizationsAssignCards}, {request.BeneficiariesAreAnonymous})");
            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var project = await db.Projects.FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

            if (project == null) throw new ProjectNotFoundException();

            request.Name.IfSet(v => project.Name = v.Trim());
            request.Url.IfSet(v => project.Url = v.Trim());
            request.CardImageFileId.IfSet(v => project.CardImageFileId = v);
            request.AllowOrganizationsAssignCards.IfSet(v => project.AllowOrganizationsAssignCards = v);
            request.BeneficiariesAreAnonymous.IfSet(v => project.BeneficiariesAreAnonymous = v);

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"Project edited {project.Name} ({project.Id})");

            return new Payload
            {
                Project = new ProjectGraphType(project)
            };
        }

        [MutationInput]
        public class Input : HaveProjectId, IRequest<Payload>
        {
            public Maybe<NonNull<string>> Name { get; set; }
            public Maybe<NonNull<string>> Url { get; set; }
            public Maybe<NonNull<string>> CardImageFileId { get; set; }
            public Maybe<bool> AllowOrganizationsAssignCards { get; set; }
            public Maybe<bool> BeneficiariesAreAnonymous { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public ProjectGraphType Project { get; set; }
        }

        public class ProjectNotFoundException : RequestValidationException { }
    }
}
