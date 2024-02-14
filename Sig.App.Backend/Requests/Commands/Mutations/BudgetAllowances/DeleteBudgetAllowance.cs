using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.BudgetAllowances
{
    public class DeleteBudgetAllowance : IRequestHandler<DeleteBudgetAllowance.Input>
    {
        private readonly ILogger<DeleteBudgetAllowance> logger;
        private readonly AppDbContext db;

        public DeleteBudgetAllowance(ILogger<DeleteBudgetAllowance> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] DeleteBudgetAllowance({request.BudgetAllowanceId})");
            var budgetAllowanceId = request.BudgetAllowanceId.LongIdentifierForType<BudgetAllowance>();
            var budgetAllowance = await db.BudgetAllowances.Include(x => x.Beneficiaries).FirstOrDefaultAsync(x => x.Id == budgetAllowanceId, cancellationToken);

            if (budgetAllowance == null) throw new BudgetAllowanceNotFoundException();

            if (HaveAnyBeneficiaries(budgetAllowance)) throw new BudgetAllowanceCantHaveBeneficiariesException();

            db.BudgetAllowances.Remove(budgetAllowance);

            await db.SaveChangesAsync();
            logger.LogInformation($"Budget allowance deleted ({budgetAllowanceId})");
        }

        private bool HaveAnyBeneficiaries(BudgetAllowance budgetAllowance)
        {
            return budgetAllowance.Beneficiaries.Any();
        }

        [MutationInput]
        public class Input : IRequest
        {
            public Id BudgetAllowanceId { get; set; }
        }

        public class BudgetAllowanceNotFoundException : RequestValidationException { }
        public class BudgetAllowanceCantHaveBeneficiariesException : RequestValidationException { }
    }
}
