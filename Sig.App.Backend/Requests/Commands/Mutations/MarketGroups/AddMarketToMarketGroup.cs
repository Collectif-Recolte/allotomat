using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Requests.Commands.Mutations.CashRegisters;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.MarketGroups
{
    public class AddMarketToMarketGroup : IRequestHandler<AddMarketToMarketGroup.Input, AddMarketToMarketGroup.Payload>
    {
        private readonly ILogger<AddMarketToMarketGroup> logger;
        private readonly AppDbContext db;
        private readonly IMediator mediator;

        public AddMarketToMarketGroup(ILogger<AddMarketToMarketGroup> logger, AppDbContext db, IMediator mediator)
        {
            this.logger = logger;
            this.db = db;
            this.mediator = mediator;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AddMarketToMarketGroup({request.MarketId}, {request.MarketGroupId})");
            var marketGroupId = request.MarketGroupId.LongIdentifierForType<MarketGroup>();
            var marketGroup = await db.MarketGroups.Include(x => x.Project).Include(x => x.Markets).FirstOrDefaultAsync(x => x.Id == marketGroupId, cancellationToken);

            if (marketGroup == null)
            {
                logger.LogWarning("[Mutation] AddMarketToMarketGroup - MarketGroupNotFoundException");
                throw new MarketGroupNotFoundException();
            }

            var marketId = request.MarketId.LongIdentifierForType<Market>();
            var market = await db.Markets.FirstOrDefaultAsync(x => x.Id == marketId, cancellationToken);

            if (market == null)
            {
                logger.LogWarning("[Mutation] AddMarketToMarketGroup - MarketNotFoundException");
                throw new MarketNotFoundException();
            }

            if (marketGroup.Markets.Any(x => x.MarketId == marketId))
            {
                logger.LogWarning("[Mutation] AddMarketToMarketGroup - MarketAlreadyInMarketGroupException");
                throw new MarketAlreadyInMarketGroupException();
            }

            marketGroup.Markets.Add(new MarketGroupMarket()
            {
                MarketId = marketId,
                MarketGroupId = marketGroupId
            });

            await db.SaveChangesAsync(cancellationToken);

            await mediator.Send(new CreateCashRegister.Input() { MarketGroupId = request.MarketGroupId, MarketId = market.GetIdentifier(), Name = "Caisse - " + marketGroup.Project.Name });

            logger.LogInformation($"[Mutation] AddMarketToMarketGroup - Market {market.Name} added to Market group {marketGroup.Name}");

            return new Payload()
            {
                MarketGroup = new MarketGroupGraphType(marketGroup)
            };
        }

        public class MarketGroupNotFoundException : RequestValidationException { }
        public class MarketNotFoundException : RequestValidationException { }
        public class MarketAlreadyInMarketGroupException : RequestValidationException { }

        [MutationInput]
        public class Input : HaveMarketGroupIdAndMarketId, IRequest<Payload> {}

        [MutationPayload]
        public class Payload
        {
            public MarketGroupGraphType MarketGroup { get; set; }
        }
    }
}
