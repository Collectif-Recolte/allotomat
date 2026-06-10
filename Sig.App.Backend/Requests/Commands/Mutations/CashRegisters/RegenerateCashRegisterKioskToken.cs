using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.CashRegisters
{
    public class RegenerateCashRegisterKioskToken : IRequestHandler<RegenerateCashRegisterKioskToken.Input, RegenerateCashRegisterKioskToken.Payload>
    {
        private readonly ILogger<RegenerateCashRegisterKioskToken> logger;
        private readonly AppDbContext db;

        public RegenerateCashRegisterKioskToken(ILogger<RegenerateCashRegisterKioskToken> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation("[Mutation] RegenerateCashRegisterKioskToken({RequestCashRegisterId})", request.CashRegisterId);
            var cashRegisterId = request.CashRegisterId.LongIdentifierForType<CashRegister>();
            var cashRegister = await db.CashRegisters.FirstOrDefaultAsync(x => x.Id == cashRegisterId, cancellationToken);

            if (cashRegister == null)
            {
                logger.LogWarning("[Mutation] RegenerateCashRegisterKioskToken - CashRegisterNotFoundException");
                throw new CashRegisterNotFoundException();
            }

            if (string.IsNullOrEmpty(cashRegister.KioskAccessToken))
            {
                logger.LogWarning("[Mutation] RegenerateCashRegisterKioskToken - KioskNotEnabledException");
                throw new KioskNotEnabledException();
            }

            cashRegister.KioskAccessToken = KioskAccessTokenHelper.Generate();
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation("[Mutation] RegenerateCashRegisterKioskToken - Token regenerated for {CashRegisterName} ({CashRegisterId})", cashRegister.Name, cashRegister.Id);

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
        public class KioskNotEnabledException : RequestValidationException { }
    }
}
