using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.BudgetAllowances;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.BudgetAllowances
{
    public class EditBudgetAllowance : IRequestHandler<EditBudgetAllowance.Input, EditBudgetAllowance.Payload>
    {
        private readonly ILogger<EditBudgetAllowance> logger;
        private readonly AppDbContext db;
        private readonly BudgetAllowanceLogFactory budgetAllowanceLogFactory;

        public EditBudgetAllowance(ILogger<EditBudgetAllowance> logger, AppDbContext db, BudgetAllowanceLogFactory budgetAllowanceLogFactory)
        {
            this.logger = logger;
            this.db = db;
            this.budgetAllowanceLogFactory = budgetAllowanceLogFactory;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] EditBudgetAllowance({request.BudgetAllowanceId}, {request.Amount})");
            var budgetAllowanceId = request.BudgetAllowanceId.LongIdentifierForType<BudgetAllowance>();
            var budgetAllowance = await db.BudgetAllowances.Include(x => x.Subscription).Include(x => x.Organization).FirstOrDefaultAsync(x => x.Id == budgetAllowanceId, cancellationToken);

            if (budgetAllowance == null)
            {
                logger.LogWarning("[Mutation] EditBudgetAllowance - BudgetAllowanceNotFoundException");
                throw new BudgetAllowanceNotFoundException();
            }

            var budgetDifference = budgetAllowance.OriginalFund - request.Amount;
            if (budgetDifference > 0)
            {
                if (budgetAllowance.AvailableFund < budgetDifference)
                {
                    logger.LogWarning("[Mutation] EditBudgetAllowance - AvailableBudgetOverNewBudgetException");
                    throw new AvailableBudgetOverNewBudgetException();
                }
            }

            budgetAllowance.AvailableFund = budgetAllowance.AvailableFund - budgetDifference;
            budgetAllowance.OriginalFund = request.Amount;

            var log = await budgetAllowanceLogFactory.CreateLog(BudgetAllowanceLogDiscriminator.EditBudgetAllowanceLog, -budgetDifference, budgetAllowance);
            db.BudgetAllowanceLogs.Add(log);

            await db.SaveChangesAsync();

            logger.LogInformation($"[Mutation] EditBudgetAllowance - Budget allowance edited {budgetAllowance.Id} ({request.Amount})");

            return new Payload()
            {
                BudgetAllowance = new BudgetAllowanceGraphType(budgetAllowance)
            };
        }

        [MutationInput]
        public class Input : IRequest<Payload>
        {
            public Id BudgetAllowanceId { get; set; }
            public decimal Amount { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public BudgetAllowanceGraphType BudgetAllowance { get; set; }
        }

        public class BudgetAllowanceNotFoundException : RequestValidationException { }
        public class AvailableBudgetOverNewBudgetException : RequestValidationException { }
    }
}
