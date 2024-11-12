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
            logger.LogInformation($"[Mutation] RemoveMarketFromProject({request.MarketId}, {request.ProjectId})");
            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var project = await db.Projects.Include(x => x.Markets).FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

            if (project == null)
            {
                logger.LogWarning("[Mutation] RemoveMarketFromProject - ProjectNotFoundException");
                throw new ProjectNotFoundException();
            }

            var marketId = request.MarketId.LongIdentifierForType<Market>();
            var market = await db.Markets.Include(x => x.MarketGroups).Include(x => x.CashRegisters).ThenInclude(x => x.MarketGroups).ThenInclude(x => x.MarketGroup).FirstOrDefaultAsync(x => x.Id == marketId, cancellationToken);

            if (market == null)
            {
                logger.LogWarning("[Mutation] RemoveMarketFromProject - MarketNotFoundException");
                throw new MarketNotFoundException();
            }

            if (!project.Markets.Any(x => x.MarketId == marketId))
            {
                logger.LogWarning("[Mutation] RemoveMarketFromProject - MarketNotInProjectException");
                throw new MarketNotInProjectException();
            }

            project.Markets.Remove(project.Markets.First(x => x.MarketId == marketId));

            foreach (var cashRegister in market.CashRegisters)
            {
                var cashRegisterMarketGroupToRemove = cashRegister.MarketGroups.Where(x => x.MarketGroup.ProjectId == projectId).ToList();
                foreach (var cashRegisterMarketGroup in cashRegisterMarketGroupToRemove)
                {
                    cashRegister.MarketGroups.Remove(cashRegisterMarketGroup);
                }

                if (cashRegister.MarketGroups.Count == 0)
                {
                    cashRegister.IsArchived = true;
                }
            }

            var marketGroupToRemove = market.MarketGroups.Where(x => x.MarketGroup.ProjectId == projectId).ToList();
            foreach (var marketGroup in marketGroupToRemove)
            {
                market.MarketGroups.Remove(marketGroup);
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] RemoveMarketFromProject - Market {market.Name} remove from project {project.Name}");

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
