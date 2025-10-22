using GraphQL.Conventions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.BudgetAllowanceLogs;
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
        private readonly IClock clock;
        private readonly AppDbContext db;
        private readonly IHttpContextAccessor httpContextAccessor;

        public DeleteBudgetAllowance(ILogger<DeleteBudgetAllowance> logger, AppDbContext db, IClock clock, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.db = db;
            this.clock = clock;
            this.httpContextAccessor = httpContextAccessor;
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

            string currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            AppUser currentUser = db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId);

            db.BudgetAllowanceLogs.Add(new BudgetAllowanceLog()
            {
                Discriminator = DbModel.Enums.BudgetAllowanceLogDiscriminator.DeleteBudgetAllowanceLog,
                CreatedAtUtc = clock.GetCurrentInstant().ToDateTimeUtc(),
                Amount = 0,
                ProjectId = budgetAllowance.Organization.ProjectId,
                BudgetAllowanceId = budgetAllowance.Id,
                InitiatorId = currentUserId,
                InitiatorEmail = currentUser?.Email,
                InitiatorFirstname = currentUser?.Profile.FirstName,
                InitiatorLastname = currentUser?.Profile.LastName,
                OrganizationId = budgetAllowance.Organization.Id,
                OrganizationName = budgetAllowance.Organization.Name,
                SubscriptionId = budgetAllowance.Subscription.Id,
                SubscriptionName = budgetAllowance.Subscription.Name
            });

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
