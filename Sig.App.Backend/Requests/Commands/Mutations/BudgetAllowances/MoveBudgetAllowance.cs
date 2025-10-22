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
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.BudgetAllowances
{
    public class MoveBudgetAllowance : IRequestHandler<MoveBudgetAllowance.Input, MoveBudgetAllowance.Payload>
    {
        private readonly ILogger<MoveBudgetAllowance> logger;
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly IHttpContextAccessor httpContextAccessor;

        public MoveBudgetAllowance(ILogger<MoveBudgetAllowance> logger, AppDbContext db, IClock clock, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.db = db;
            this.clock = clock;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] MoveBudgetAllowance({request.InitialBudgetAllowanceId}, {request.TargetBudgetAllowanceId}, {request.Amount})");
            var initialBudgetAllowanceId = request.InitialBudgetAllowanceId.LongIdentifierForType<BudgetAllowance>();
            var initialBudgetAllowance = await db.BudgetAllowances.Include(x => x.Organization).Include(x => x.Subscription).FirstOrDefaultAsync(x => x.Id == initialBudgetAllowanceId, cancellationToken);

            if (initialBudgetAllowance == null)
            {
                logger.LogWarning("[Mutation] MoveBudgetAllowance - InitialBudgetAllowanceNotFoundException");
                throw new InitialBudgetAllowanceNotFoundException();
            }

            var targetBudgetAllowanceId = request.TargetBudgetAllowanceId.LongIdentifierForType<BudgetAllowance>();
            var targetBudgetAllowance = await db.BudgetAllowances.Include(x => x.Organization).Include(x => x.Subscription).FirstOrDefaultAsync(x => x.Id == targetBudgetAllowanceId, cancellationToken);

            if (targetBudgetAllowance == null)
            {
                logger.LogWarning("[Mutation] MoveBudgetAllowance - TargetBudgetAllowanceNotFoundException");
                throw new TargetBudgetAllowanceNotFoundException();
            }

            if (initialBudgetAllowance.AvailableFund < request.Amount)
            {
                logger.LogWarning("[Mutation] MoveBudgetAllowance - AvailableBudgetUnderRequestAmountException");
                throw new AvailableBudgetUnderRequestAmountException();
            }

            initialBudgetAllowance.AvailableFund -= request.Amount;
            initialBudgetAllowance.OriginalFund -= request.Amount;

            targetBudgetAllowance.AvailableFund += request.Amount;
            targetBudgetAllowance.OriginalFund += request.Amount;

            string currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            AppUser currentUser = db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId);

            db.BudgetAllowanceLogs.Add(new BudgetAllowanceLog()
            {
                Discriminator = DbModel.Enums.BudgetAllowanceLogDiscriminator.MoveBudgetAllowanceLog,
                CreatedAtUtc = clock.GetCurrentInstant().ToDateTimeUtc(),
                Amount = request.Amount,
                ProjectId = initialBudgetAllowance.Organization.ProjectId,
                BudgetAllowanceId = initialBudgetAllowance.Id,
                InitiatorId = currentUserId,
                InitiatorEmail = currentUser?.Email,
                InitiatorFirstname = currentUser?.Profile.FirstName,
                InitiatorLastname = currentUser?.Profile.LastName,
                OrganizationId = initialBudgetAllowance.Organization.Id,
                OrganizationName = initialBudgetAllowance.Organization.Name,
                SubscriptionId = initialBudgetAllowance.Subscription.Id,
                SubscriptionName = initialBudgetAllowance.Subscription.Name,
                TargetBudgetAllowanceId = targetBudgetAllowance.Id,
                TargetOrganizationId = targetBudgetAllowance.Organization.Id,
                TargetOrganizationName = targetBudgetAllowance.Organization.Name,
                TargetSubscriptionId = targetBudgetAllowance.Subscription.Id,
                TargetSubscriptionName = targetBudgetAllowance.Subscription.Name
            });

            await db.SaveChangesAsync();

            logger.LogInformation($"[Mutation] MoveBudgetAllowance - Budget allowance move from {initialBudgetAllowance.Id} to {targetBudgetAllowance.Id} ({request.Amount})");

            return new Payload()
            {
                InitialBudgetAllowance = new BudgetAllowanceGraphType(initialBudgetAllowance),
                TargetBudgetAllowance = new BudgetAllowanceGraphType(initialBudgetAllowance)
            };
        }

        [MutationInput]
        public class Input : IRequest<Payload>
        {
            public Id InitialBudgetAllowanceId { get; set; }
            public Id TargetBudgetAllowanceId { get; set; }
            public decimal Amount { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public BudgetAllowanceGraphType InitialBudgetAllowance { get; set; }
            public BudgetAllowanceGraphType TargetBudgetAllowance { get; set; }
        }

        public class InitialBudgetAllowanceNotFoundException : RequestValidationException { }
        public class TargetBudgetAllowanceNotFoundException : RequestValidationException { }
        public class AvailableBudgetUnderRequestAmountException : RequestValidationException { }
    }
}
