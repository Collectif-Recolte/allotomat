using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Plugins.GraphQL;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.MediatR;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Extensions;
using GraphQL.Conventions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using System.Linq;
using Sig.App.Backend.Gql.Bases;

namespace Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries
{
    public class AddBeneficiaryTypeInProject : IRequestHandler<AddBeneficiaryTypeInProject.Input, AddBeneficiaryTypeInProject.Payload>
    {
        private readonly ILogger<AddBeneficiaryTypeInProject> logger;
        private readonly AppDbContext db;

        public AddBeneficiaryTypeInProject(ILogger<AddBeneficiaryTypeInProject> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AddBeneficiaryTypeInProject({request.ProjectId}, {request.Name}, {request.Keys})");
            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var project = await db.Projects.Include(x => x.Markets).FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

            if (project == null) throw new ProjectNotFoundException();

            var beneficiaryTypes = await db.BeneficiaryTypes.Where(x => x.ProjectId == projectId).ToListAsync();
            var beneficiaryTypesKeys = beneficiaryTypes.SelectMany(x => x.GetKeys());

            if (request.Keys.Where(x => beneficiaryTypesKeys.Contains(x.Trim().ToLower())).Any()) throw new BeneficiaryTypeKeyAlreadyInUseException();

            var beneficiaryType = new BeneficiaryType()
            {
                Name = request.Name.Trim(),
                Project = project
            };
            beneficiaryType.SetKeys(request.Keys);
            
            db.BeneficiaryTypes.Add(beneficiaryType);

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"New beneficiary type created {beneficiaryType.Name} ({beneficiaryType.Id})");

            return new Payload
            {
                BeneficiaryType = new BeneficiaryTypeGraphType(beneficiaryType)
            };
        }

        [MutationInput]
        public class Input : HaveProjectId, IRequest<Payload>
        {
            public string Name { get; set; }
            public string[] Keys { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public BeneficiaryTypeGraphType BeneficiaryType { get; set; }
        }

        public class ProjectNotFoundException : RequestValidationException { }
        public class BeneficiaryTypeKeyAlreadyInUseException : RequestValidationException { }
    }
}
