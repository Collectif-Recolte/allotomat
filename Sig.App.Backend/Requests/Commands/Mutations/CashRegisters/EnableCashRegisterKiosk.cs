using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Services.Kiosk;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.CashRegisters
{
    public class EnableCashRegisterKiosk : IRequestHandler<EnableCashRegisterKiosk.Input, EnableCashRegisterKiosk.Payload>
    {
        private readonly ILogger<EnableCashRegisterKiosk> logger;
        private readonly AppDbContext db;

        public EnableCashRegisterKiosk(ILogger<EnableCashRegisterKiosk> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation("[Mutation] EnableCashRegisterKiosk({RequestCashRegisterId})", request.CashRegisterId);
            var cashRegister = await GetCashRegister(request, cancellationToken);

            if (!string.IsNullOrEmpty(cashRegister.KioskAccessToken))
            {
                logger.LogWarning("[Mutation] EnableCashRegisterKiosk - KioskAlreadyEnabledException");
                throw new KioskAlreadyEnabledException();
            }

            cashRegister.KioskAccessToken = KioskHelper.GenerateAccessToken();
            cashRegister.KioskPassword = KioskHelper.GeneratePassword();
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[Mutation] EnableCashRegisterKiosk - Kiosk enabled for {CashRegisterName} ({CashRegisterId})", cashRegister.Name, cashRegister.Id);

            return new Payload
            {
                CashRegister = new CashRegisterGraphType(cashRegister)
            };
        }

        private async Task<CashRegister> GetCashRegister(Input request, CancellationToken cancellationToken)
        {
            var cashRegisterId = request.CashRegisterId.LongIdentifierForType<CashRegister>();
            var cashRegister = await db.CashRegisters.FirstOrDefaultAsync(x => x.Id == cashRegisterId, cancellationToken);

            if (cashRegister == null)
            {
                logger.LogWarning("[Mutation] EnableCashRegisterKiosk - CashRegisterNotFoundException");
                throw new CashRegisterNotFoundException();
            }

            return cashRegister;
        }

        [MutationInput]
        public class Input : HaveCashRegisterId, IRequest<Payload> { }

        [MutationPayload]
        public class Payload
        {
            public CashRegisterGraphType CashRegister { get; set; }
        }

        public class CashRegisterNotFoundException : RequestValidationException { }
        public class KioskAlreadyEnabledException : RequestValidationException { }
    }
}
