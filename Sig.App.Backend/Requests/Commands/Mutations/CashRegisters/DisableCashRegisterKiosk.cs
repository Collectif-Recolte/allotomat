using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.CashRegisters
{
    public class DisableCashRegisterKiosk : IRequestHandler<DisableCashRegisterKiosk.Input, DisableCashRegisterKiosk.Payload>
    {
        private readonly ILogger<DisableCashRegisterKiosk> logger;
        private readonly AppDbContext db;

        public DisableCashRegisterKiosk(ILogger<DisableCashRegisterKiosk> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation("[Mutation] DisableCashRegisterKiosk({RequestCashRegisterId})", request.CashRegisterId);
            var cashRegisterId = request.CashRegisterId.LongIdentifierForType<CashRegister>();
            var cashRegister = await db.CashRegisters.FirstOrDefaultAsync(x => x.Id == cashRegisterId, cancellationToken);

            if (cashRegister == null)
            {
                logger.LogWarning("[Mutation] DisableCashRegisterKiosk - CashRegisterNotFoundException");
                throw new CashRegisterNotFoundException();
            }

            cashRegister.KioskAccessToken = null;
            cashRegister.KioskPassword = null;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[Mutation] DisableCashRegisterKiosk - Kiosk disabled for {CashRegisterName} ({CashRegisterId})", cashRegister.Name, cashRegister.Id);

            return new Payload
            {
                CashRegister = new CashRegisterGraphType(cashRegister)
            };
        }

        [MutationInput]
        public class Input : HaveCashRegisterId, IRequest<Payload> { }

        [MutationPayload]
        public class Payload
        {
            public CashRegisterGraphType CashRegister { get; set; }
        }

        public class CashRegisterNotFoundException : RequestValidationException { }
    }
}
