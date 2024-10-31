using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.CashRegisters
{
    public class CreateCashRegister : IRequestHandler<CreateCashRegister.Input, CreateCashRegister.Payload>
    {
        private readonly ILogger<CreateCashRegister> logger;
        private readonly AppDbContext db;

        public CreateCashRegister(ILogger<CreateCashRegister> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] CreateCashRegister({request.MarketId}, {request.Name})");

            var marketId = request.MarketId.LongIdentifierForType<Market>();
            var market = await db.Markets.FirstOrDefaultAsync(x => x.Id == marketId, cancellationToken);

            if (market == null)
            {
                logger.LogWarning("[Mutation] CreateCashRegister - MarketNotFoundException");
                throw new MarketNotFoundException();
            }

            var marketGroupId = request.MarketGroupId.LongIdentifierForType<MarketGroup>();
            var marketGroup = await db.MarketGroups.Include(x => x.Markets).FirstOrDefaultAsync(x => x.Id == marketGroupId, cancellationToken);

            if (marketGroup == null)
            {
                logger.LogWarning("[Mutation] CreateCashRegister - MarketGroupNotFoundException");
                throw new MarketGroupNotFoundException();
            }

            if (!marketGroup.Markets.Select(x => x.Market).Contains(market))
            {
                logger.LogWarning("[Mutation] CreateCashRegister - MarketGroupNotFoundInMarketException");
                throw new MarketGroupNotFoundInMarketException();
            }

            var cashRegister = new CashRegister()
            {
                Name = request.Name.Trim(),
                Market = market,
                MarketGroups = new List<CashRegisterMarketGroup>()
            };
            cashRegister.MarketGroups.Add(new CashRegisterMarketGroup()
            {
                CashRegister = cashRegister,
                MarketGroup = marketGroup
            });

            db.CashRegisters.Add(cashRegister);

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] CreateCashRegister - New cash register created {cashRegister.Name} ({cashRegister.Id})");

            return new Payload
            {
                CashRegister = new CashRegisterGraphType(cashRegister)
            };
        }

        [MutationInput]
        public class Input : HaveMarketId, IRequest<Payload>
        {
            public string Name { get; set; }
            public Id MarketGroupId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public CashRegisterGraphType CashRegister { get; set; }
        }

        public class MarketNotFoundException : RequestValidationException { }
        public class MarketGroupNotFoundException : RequestValidationException { }
        public class MarketGroupNotFoundInMarketException : RequestValidationException { }
    }
}
