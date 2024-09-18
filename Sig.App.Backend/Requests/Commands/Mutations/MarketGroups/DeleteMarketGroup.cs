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
    public class DeleteMarketGroup : IRequestHandler<DeleteMarketGroup.Input>
    {
        private readonly ILogger<DeleteMarketGroup> logger;
        private readonly UserManager<AppUser> userManager;
        private readonly AppDbContext db;
        private readonly IMediator mediator;

        public DeleteMarketGroup(ILogger<DeleteMarketGroup> logger, AppDbContext db, UserManager<AppUser> userManager, IMediator mediator)
        {
            this.logger = logger;
            this.userManager = userManager;
            this.db = db;
            this.mediator = mediator;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] DeleteMarketGroup({request.MarketGroupId})");
            var marketGroupId = request.MarketGroupId.LongIdentifierForType<MarketGroup>();
            var marketGroup = await db.MarketGroups
                .Include(x => x.Markets)
                .FirstOrDefaultAsync(x => x.Id == marketGroupId, cancellationToken);

            if (marketGroup == null)
            {
                logger.LogWarning("[Mutation] DeleteMarketGroup - MarketGroupNotFoundException");
                throw new MarketGroupNotFoundException();
            }

            var marketManagers = await mediator.Send(new GetMarketGroupManagers.Query
            {
                MarketGroupId = marketGroupId
            });

            if (marketManagers != null) {
                foreach (var manager in marketManagers)
                {
                    await userManager.RemoveClaimAsync(manager, new Claim(AppClaimTypes.MarketGroupManagerOf, marketGroupId.ToString()));
                    logger.LogInformation($"[Mutation] DeleteMarketGroup - Remove claim from manager {manager.Email}");
                }
            }

            // TODO Remove transactions for this market group
            /*
            var transactions = await db.Transactions.ToListAsync();
            transactions = transactions.Where(x => x.GetType() == typeof(PaymentTransaction) && (x as PaymentTransaction).MarketId == marketId).ToList();

            if (transactions.Count() > 0)
            {
                db.Transactions.RemoveRange(transactions);
            }
            */

            db.MarketGroupMarkets.RemoveRange(marketGroup.Markets);
            db.MarketGroups.Remove(marketGroup);

            await db.SaveChangesAsync();
            logger.LogInformation($"[Mutation] DeleteMarketGroup - Market group deleted ({marketGroupId}, {marketGroup.Name})");
        }

        [MutationInput]
        public class Input : HaveMarketGroupId, IRequest {}

        public class MarketGroupNotFoundException : RequestValidationException {}
    }
}
