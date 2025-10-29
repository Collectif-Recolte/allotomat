using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Plugins.BudgetAllowances;
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
        private readonly BudgetAllowanceLogFactory budgetAllowanceLogFactory;

        public DeleteBudgetAllowance(ILogger<DeleteBudgetAllowance> logger, AppDbContext db, BudgetAllowanceLogFactory budgetAllowanceLogFactory)
        {
            this.logger = logger;
            this.db = db;
            this.budgetAllowanceLogFactory = budgetAllowanceLogFactory;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] DeleteBudgetAllowance({request.BudgetAllowanceId})");
            var budgetAllowanceId = request.BudgetAllowanceId.LongIdentifierForType<BudgetAllowance>();
            var budgetAllowance = await db.BudgetAllowances.Include(x => x.Beneficiaries).Include(x => x.Organization).Include(x => x.Subscription).FirstOrDefaultAsync(x => x.Id == budgetAllowanceId, cancellationToken);

            if (budgetAllowance == null)
            {
                logger.LogWarning("[Mutation] DeleteBudgetAllowance - BudgetAllowanceNotFoundException");
                throw new BudgetAllowanceNotFoundException();
            }

            if (HaveAnyBeneficiaries(budgetAllowance))
            {
                logger.LogWarning("[Mutation] DeleteBudgetAllowance - BudgetAllowanceCantHaveBeneficiariesException");
                throw new BudgetAllowanceCantHaveBeneficiariesException();
            }

            db.BudgetAllowances.Remove(budgetAllowance);

            var log = await budgetAllowanceLogFactory.CreateLog(BudgetAllowanceLogDiscriminator.DeleteBudgetAllowanceLog, budgetAllowance.AvailableFund, budgetAllowance);
            db.BudgetAllowanceLogs.Add(log);

            await db.SaveChangesAsync();
            logger.LogInformation($"[Mutation] DeleteBudgetAllowance - Budget allowance deleted ({budgetAllowanceId})");
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
