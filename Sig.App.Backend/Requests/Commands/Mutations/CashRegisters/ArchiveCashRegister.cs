using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Plugins.GraphQL;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Extensions;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.DbModel.Entities.CashRegisters;

namespace Sig.App.Backend.Requests.Commands.Mutations.CashRegisters
{
    public class ArchiveCashRegister : IRequestHandler<ArchiveCashRegister.Input>
    {
        private readonly ILogger<ArchiveCashRegister> logger;
        private readonly AppDbContext db;

        public ArchiveCashRegister(ILogger<ArchiveCashRegister> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] ArchiveCashRegister({request.CashRegisterId})");
            var cashRegisterId = request.CashRegisterId.LongIdentifierForType<CashRegister>();
            var cashRegister = await db.CashRegisters
                .Include(x => x.MarketGroups)
                .FirstOrDefaultAsync(x => x.Id == cashRegisterId, cancellationToken);

            if (cashRegister == null)
            {
                logger.LogWarning("[Mutation] ArchiveCashRegister - CashRegisterNotFoundException");
                throw new CashRegisterNotFoundException();
            }

            db.CashRegisterMarketGroups.RemoveRange(cashRegister.MarketGroups);

            cashRegister.IsArchived = true;

            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation($"[Mutation] ArchiveCashRegister - Cash register archive ({cashRegisterId}, {cashRegister.Name})");
        }

        [MutationInput]
        public class Input : HaveCashRegisterId, IRequest {}

        public class CashRegisterNotFoundException : RequestValidationException { }
    }
}
