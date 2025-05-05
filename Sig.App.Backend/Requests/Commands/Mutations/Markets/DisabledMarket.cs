using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Markets
{
    public class DisabledMarket : IRequestHandler<DisabledMarket.Input, DisabledMarket.Payload>
    {
        private readonly ILogger<DisabledMarket> logger;
        private readonly AppDbContext db;

        public DisabledMarket(ILogger<DisabledMarket> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] DisabledMarket({request.Name})");
            var marketId = request.MarketId.LongIdentifierForType<Market>();
            var market = await db.Markets.FirstOrDefaultAsync(x => x.Id == marketId, cancellationToken);

            if (market == null)
            {
                logger.LogWarning("[Mutation] DisabledMarket - MarketNotFoundException");
                throw new MarketNotFoundException();
            }

            market.IsDisabled = true;

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] DisabledMarket - Market disabled {market.Name} ({market.Id})");

            return new Payload
            {
                Market = new MarketGraphType(market)
            };
        }

        [MutationInput]
        public class Input : HaveMarketId, IRequest<Payload>
        {
            public Maybe<NonNull<string>> Name { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public MarketGraphType Market { get; set; }
        }

        public class MarketNotFoundException : RequestValidationException { }
    }
}
