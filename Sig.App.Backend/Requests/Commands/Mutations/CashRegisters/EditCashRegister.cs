using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.CashRegisters
{
    public class EditCashRegister : IRequestHandler<EditCashRegister.Input, EditCashRegister.Payload>
    {
        private readonly ILogger<EditCashRegister> logger;
        private readonly AppDbContext db;

        public EditCashRegister(ILogger<EditCashRegister> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] EditCashRegister({request.Name})");
            var cashRegisterId = request.CashRegisterId.LongIdentifierForType<CashRegister>();
            var cashRegister = await db.CashRegisters.FirstOrDefaultAsync(x => x.Id == cashRegisterId, cancellationToken);

            if (cashRegister == null)
            {
                logger.LogWarning("[Mutation] EditCashRegister - CashRegisterNotFoundException");
                throw new CashRegisterNotFoundException();
            }

            request.Name.IfSet(v => cashRegister.Name = v.Trim());

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] EditCashRegister - Cash register edited {cashRegister.Name} ({cashRegister.Id})");

            return new Payload
            {
                CashRegister = new CashRegisterGraphType(cashRegister)
            };
        }

        [MutationInput]
        public class Input : HaveCashRegisterId, IRequest<Payload>
        {
            public Maybe<NonNull<string>> Name { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public CashRegisterGraphType CashRegister { get; set; }
        }

        public class CashRegisterNotFoundException : RequestValidationException { }
    }
}
