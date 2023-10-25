using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.BudgetAllowances
{
    public class CreateBudgetAllowance : IRequestHandler<CreateBudgetAllowance.Input, CreateBudgetAllowance.Payload>
    {
        private readonly ILogger<CreateBudgetAllowance> logger;
        private readonly AppDbContext db;
        
        public CreateBudgetAllowance(ILogger<CreateBudgetAllowance> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations.Include(x => x.BudgetAllowances).FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null) throw new OrganizationNotFoundException();

            var subscriptionId = request.SubscriptionId.LongIdentifierForType<Subscription>();
            var subscription = await db.Subscriptions.FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

            if (subscription == null) throw new SubscriptionNotFoundException();

            if (subscription.ProjectId != organization.ProjectId) throw new OrganizationAndSubscriptionNotRelated();

            if (organization.BudgetAllowances.Any(x => x.SubscriptionId == subscriptionId)) throw new OrganizationAlreadyHaveBudgetForSubscriptionException();

            var budgetAllowance = new BudgetAllowance()
            {
                Organization = organization,
                Subscription = subscription,
                OriginalFund = request.Amount,
                AvailableFund = request.Amount
            };

            db.BudgetAllowances.Add(budgetAllowance);
            await db.SaveChangesAsync();

            logger.LogInformation($"New budget allowance created for {organization.Name} ({request.Amount})");

            return new Payload()
            {
                BudgetAllowance = new BudgetAllowanceGraphType(budgetAllowance)
            };
        }

        [MutationInput]
        public class Input : IRequest<Payload>, IHaveOrganizationId, IHaveSubscriptionId
        {
            public Id OrganizationId { get; set; }
            public Id SubscriptionId { get; set; }
            public decimal Amount { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public BudgetAllowanceGraphType BudgetAllowance { get; set; }
        }

        public class OrganizationNotFoundException : RequestValidationException { }
        public class SubscriptionNotFoundException : RequestValidationException { }
        public class OrganizationAndSubscriptionNotRelated : RequestValidationException { }
        public class OrganizationAlreadyHaveBudgetForSubscriptionException : RequestValidationException { }
    }
}
