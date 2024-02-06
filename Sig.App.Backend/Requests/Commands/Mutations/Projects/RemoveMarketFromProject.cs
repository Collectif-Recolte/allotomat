using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Projects
{
    public class RemoveMarketFromProject : IRequestHandler<RemoveMarketFromProject.Input, RemoveMarketFromProject.Payload>
    {
        private readonly ILogger<RemoveMarketFromProject> logger;
        private readonly AppDbContext db;

        public RemoveMarketFromProject(ILogger<RemoveMarketFromProject> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var project = await db.Projects.Include(x => x.Markets).FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

            if (project == null) throw new ProjectNotFoundException();

            var marketId = request.MarketId.LongIdentifierForType<Market>();
            var market = await db.Markets.FirstOrDefaultAsync(x => x.Id == marketId, cancellationToken);

            if (market == null) throw new MarketNotFoundException();

            if (!project.Markets.Any(x => x.MarketId == marketId)) throw new MarketNotInProjectException();

            project.Markets.Remove(project.Markets.First(x => x.MarketId == marketId));

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"Market {market.Name} remove from project {project.Name}");

            return new Payload()
            {
                Project = new ProjectGraphType(project)
            };
        }

        public class ProjectNotFoundException : RequestValidationException { }
        public class MarketNotFoundException : RequestValidationException { }
        public class MarketNotInProjectException : RequestValidationException { }

        [MutationInput]
        public class Input : HaveProjectIdAndMarketId, IRequest<Payload> {}

        [MutationPayload]
        public class Payload
        {
            public ProjectGraphType Project { get; set; }
        }
    }
}
