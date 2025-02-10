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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.MarketGroups
{
    public class RemoveMarketFromMarketGroup : IRequestHandler<RemoveMarketFromMarketGroup.Input, RemoveMarketFromMarketGroup.Payload>
    {
        private readonly ILogger<RemoveMarketFromMarketGroup> logger;
        private readonly AppDbContext db;

        public RemoveMarketFromMarketGroup(ILogger<RemoveMarketFromMarketGroup> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] RemoveMarketFromMarketGroup({request.MarketId}, {request.MarketGroupId})");
            var marketGroupId = request.MarketGroupId.LongIdentifierForType<MarketGroup>();
            var marketGroup = await db.MarketGroups.Include(x => x.Markets).Include(x => x.CashRegisters).FirstOrDefaultAsync(x => x.Id == marketGroupId, cancellationToken);

            if (marketGroup == null)
            {
                logger.LogWarning("[Mutation] RemoveMarketFromMarketGroup - MarketGroupNotFoundException");
                throw new MarketGroupNotFoundException();
            }

            var marketId = request.MarketId.LongIdentifierForType<Market>();
            var market = await db.Markets.Include(x => x.MarketGroups).Include(x => x.CashRegisters).ThenInclude(x => x.MarketGroups).ThenInclude(x => x.MarketGroup).FirstOrDefaultAsync(x => x.Id == marketId, cancellationToken);

            if (market == null)
            {
                logger.LogWarning("[Mutation] RemoveMarketFromMarketGroup - MarketNotFoundException");
                throw new MarketNotFoundException();
            }

            if (!marketGroup.Markets.Any(x => x.MarketId == marketId))
            {
                logger.LogWarning("[Mutation] RemoveMarketFromMarketGroup - MarketNotInMarketGroupException");
                throw new MarketNotInMarketGroupException();
            }

            marketGroup.Markets.Remove(marketGroup.Markets.First(x => x.MarketId == marketId));

            foreach (var cashRegister in market.CashRegisters)
            {
                var cashRegisterMarketGroupToRemove = cashRegister.MarketGroups.Where(x => x.MarketGroup.Id == marketGroupId).ToList();
                foreach (var cashRegisterMarketGroup in cashRegisterMarketGroupToRemove)
                {
                    cashRegister.MarketGroups.Remove(cashRegisterMarketGroup);
                }

                if (cashRegister.MarketGroups.Count == 0)
                {
                    cashRegister.IsArchived = true;
                }
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] RemoveMarketFromMarketGroup - Market {market.Name} remove from market group {marketGroup.Name}");

            return new Payload()
            {
                MarketGroup = new MarketGroupGraphType(marketGroup)
            };
        }

        public class MarketGroupNotFoundException : RequestValidationException { }
        public class MarketNotFoundException : RequestValidationException { }
        public class MarketNotInMarketGroupException : RequestValidationException { }

        [MutationInput]
        public class Input : HaveMarketGroupIdAndMarketId, IRequest<Payload> {}

        [MutationPayload]
        public class Payload
        {
            public MarketGroupGraphType MarketGroup { get; set; }
        }
    }
}
