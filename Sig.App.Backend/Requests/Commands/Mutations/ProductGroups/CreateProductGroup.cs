using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.ProductGroups
{
    public class CreateProductGroup : IRequestHandler<CreateProductGroup.Input, CreateProductGroup.Payload>
    {
        private readonly ILogger<CreateProductGroup> logger;
        private readonly AppDbContext db;

        public CreateProductGroup(ILogger<CreateProductGroup> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] CreateProductGroup({request.ProjectId}, {request.Name}, {request.Color}, {request.OrderOfAppearance})");
            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var project = await db.Projects.Include(x => x.ProductGroups).FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

            if (project == null) throw new ProjectNotFoundException();

            if (request.Name == ProductGroupType.LOYALTY) throw new CantCreateProductGroupWithLoyaltyDefaultName();

            var productGroup = new ProductGroup()
            {
                Project = project,
                Color = request.Color,
                Name = request.Name,
                OrderOfAppearance = request.OrderOfAppearance
            };

            db.ProductGroups.Add(productGroup);
            await db.SaveChangesAsync();

            logger.LogInformation($"New product group created for {project.Name} ({request.Name})");

            return new Payload()
            {
                ProductGroup = new ProductGroupGraphType(productGroup)
            };
        }

        [MutationInput]
        public class Input : HaveProjectId, IRequest<Payload>
        {
            public string Name { get; set; }
            public ProductGroupColor Color { get; set; }
            public int OrderOfAppearance { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public ProductGroupGraphType ProductGroup { get; set; }
        }

        public class ProjectNotFoundException : RequestValidationException { }
        public class CantCreateProductGroupWithLoyaltyDefaultName : RequestValidationException { }
    }
}
