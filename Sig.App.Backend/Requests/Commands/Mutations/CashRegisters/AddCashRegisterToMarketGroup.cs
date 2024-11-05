using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.CashRegisters
{
    public class AddCashRegisterToMarketGroup : IRequestHandler<AddCashRegisterToMarketGroup.Input, AddCashRegisterToMarketGroup.Payload>
    {
        private readonly ILogger<AddCashRegisterToMarketGroup> logger;
        private readonly AppDbContext db;

        public AddCashRegisterToMarketGroup(ILogger<AddCashRegisterToMarketGroup> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AddCashRegisterToMarketGroup({request.MarketGroupId}, {request.CashRegisterId})");

            var marketGroupId = request.MarketGroupId.LongIdentifierForType<MarketGroup>();
            var marketGroup = await db.MarketGroups.Include(x => x.Markets).FirstOrDefaultAsync(x => x.Id == marketGroupId, cancellationToken);

            if (marketGroup == null)
            {
                logger.LogWarning("[Mutation] AddCashRegisterToMarketGroup - MarketGroupNotFoundException");
                throw new MarketGroupNotFoundException();
            }

            var cashRegisterId = request.CashRegisterId.LongIdentifierForType<CashRegister>();
            var cashRegister = await db.CashRegisters.Include(x => x.MarketGroups).FirstOrDefaultAsync(x => x.Id == cashRegisterId, cancellationToken);

            if (cashRegister == null)
            {
                logger.LogWarning("[Mutation] AddCashRegisterToMarketGroup - CashRegisterNotFoundException");
                throw new CashRegisterNotFoundException();
            }

            if (cashRegister.MarketGroups.Any(x => x.MarketGroupId == marketGroupId))
            {
                logger.LogInformation("[Mutation] AddCashRegisterToMarketGroup - CashRegisterAlreadyInMarketGroupException");
                throw new CashRegisterAlreadyInMarketGroupException();
            }

            cashRegister.MarketGroups.Add(new CashRegisterMarketGroup()
            {
                CashRegister = cashRegister,
                MarketGroup = marketGroup
            });

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] AddCashRegisterToMarketGroup - Cash register ({cashRegister.Name}) added to MarketGroup ({marketGroup.Name})");

            return new Payload
            {
                CashRegister = new CashRegisterGraphType(cashRegister)
            };
        }

        [MutationInput]
        public class Input : HaveCashRegisterId, IRequest<Payload>
        {
            public Id MarketGroupId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public CashRegisterGraphType CashRegister { get; set; }
        }

        public class MarketGroupNotFoundException : RequestValidationException { }
        public class CashRegisterNotFoundException : RequestValidationException { }
        public class CashRegisterAlreadyInMarketGroupException : RequestValidationException { }
    }
}
