using GraphQL.Conventions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Markets
{
    public class RemoveManagerFromMarket : IRequestHandler<RemoveManagerFromMarket.Input, RemoveManagerFromMarket.Payload>
    {
        private readonly ILogger<RemoveManagerFromMarket> logger;
        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;

        public RemoveManagerFromMarket(ILogger<RemoveManagerFromMarket> logger, AppDbContext db, UserManager<AppUser> userManager)
        {
            this.logger = logger;
            this.db = db;
            this.userManager = userManager;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] RemoveManagerFromMarket({request.MarketId}, {request.ManagerId})");
            var marketId = request.MarketId.LongIdentifierForType<Market>();
            var market = await db.Markets.FirstOrDefaultAsync(x => x.Id == marketId, cancellationToken);

            if (market == null)
            {
                logger.LogWarning("[Mutation] RemoveManagerFromMarket - MarketNotFoundException");
                throw new MarketNotFoundException();
            }
            
            var manager = await db.Users.FirstOrDefaultAsync(x => x.Id == request.ManagerId.IdentifierForType<AppUser>());

            if (manager == null)
            {
                logger.LogWarning("[Mutation] RemoveManagerFromMarket - ManagerNotFoundException");
                throw new ManagerNotFoundException();
            }

            await userManager.RemoveClaimAsync(manager, new Claim(AppClaimTypes.MarketManagerOf, market.Id.ToString()));

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"Market manager {manager.Email} remove from market {market.Name} ({market.Id})");

            return new Payload
            {
                Market = new MarketGraphType(market)
            };
        }

        public class MarketNotFoundException : RequestValidationException { }
        public class ManagerNotFoundException : RequestValidationException { }


        [MutationInput]
        public class Input : HaveMarketId, IRequest<Payload>
        {
            public Id ManagerId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public MarketGraphType Market { get; set; }
        }
    }
}