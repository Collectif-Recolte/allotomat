using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Plugins.GraphQL;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Sig.App.Backend.Constants;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.Requests.Queries.MarketGroups;

namespace Sig.App.Backend.Requests.Commands.Mutations.MarketGroups
{
    public class ArchiveMarketGroup : IRequestHandler<ArchiveMarketGroup.Input>
    {
        private readonly ILogger<ArchiveMarketGroup> logger;
        private readonly UserManager<AppUser> userManager;
        private readonly AppDbContext db;
        private readonly IMediator mediator;

        public ArchiveMarketGroup(ILogger<ArchiveMarketGroup> logger, AppDbContext db, UserManager<AppUser> userManager, IMediator mediator)
        {
            this.logger = logger;
            this.userManager = userManager;
            this.db = db;
            this.mediator = mediator;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] ArchiveMarketGroup({request.MarketGroupId})");
            var marketGroupId = request.MarketGroupId.LongIdentifierForType<MarketGroup>();
            var marketGroup = await db.MarketGroups
                .Include(x => x.Markets)
                .FirstOrDefaultAsync(x => x.Id == marketGroupId, cancellationToken);

            if (marketGroup == null)
            {
                logger.LogWarning("[Mutation] ArchiveMarketGroup - MarketGroupNotFoundException");
                throw new MarketGroupNotFoundException();
            }

            var marketGroupManagers = await mediator.Send(new GetMarketGroupManagers.Query
            {
                MarketGroupId = marketGroupId
            });

            if (marketGroupManagers != null) {
                foreach (var manager in marketGroupManagers)
                {
                    await userManager.RemoveClaimAsync(manager, new Claim(AppClaimTypes.MarketGroupManagerOf, marketGroup.Id.ToString()));
                    logger.LogInformation($"[Mutation] ArchiveMarketGroup - Remove claim for manager {manager.Email}");
                }
            }

            db.MarketGroupMarkets.RemoveRange(marketGroup.Markets);

            marketGroup.IsArchived = true;

            await db.SaveChangesAsync();
            logger.LogInformation($"[Mutation] ArchiveMarketGroup - Market archive ({marketGroupId}, {marketGroup.Name})");
        }

        [MutationInput]
        public class Input : HaveMarketGroupId, IRequest {}

        public class MarketGroupNotFoundException : RequestValidationException { }
    }
}
