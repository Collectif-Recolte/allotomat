using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Plugins.GraphQL;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.Plugins.MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.Gql.Bases;

namespace Sig.App.Backend.Requests.Commands.Mutations.Subscriptions
{
    public class DeleteSubscription : IRequestHandler<DeleteSubscription.Input>
    {
        private readonly ILogger<DeleteSubscription> logger;
        private readonly AppDbContext db;

        public DeleteSubscription(ILogger<DeleteSubscription> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] DeleteSubscription({request.SubscriptionId})");
            var subscriptionId = request.SubscriptionId.LongIdentifierForType<Subscription>();
            var subscription = await db.Subscriptions.Include(x => x.Types).Include(x => x.Beneficiaries).Include(x=> x.BudgetAllowances).FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

            if (subscription == null)
            {
                logger.LogWarning("[Mutation] DeleteSubscription - SubscriptionNotFoundException");
                throw new SubscriptionNotFoundException();
            }

            if (db.SubscriptionBeneficiaries.Where(x => x.SubscriptionId == subscriptionId).Any())
            {
                logger.LogWarning("[Mutation] DeleteSubscription - CantDeleteSubscriptionWithBeneficiaries");
                throw new CantDeleteSubscriptionWithBeneficiaries();
            }

            var subscriptionTypeIds = subscription.Types.Select(y => y.Id).ToArray();

            var transactions = await db.Transactions.ToListAsync();
            var addingFundTransactions = transactions.Where(x => x.GetType() == typeof(SubscriptionAddingFundTransaction) && subscriptionTypeIds.Contains((x as SubscriptionAddingFundTransaction).SubscriptionTypeId)).ToList();
            var manuallyAddingFundTransactions = transactions.Where(x => x.GetType() == typeof(ManuallyAddingFundTransaction) && subscription.Id  == (x as ManuallyAddingFundTransaction).SubscriptionId).ToList();

            if (addingFundTransactions.Count() > 0)
            {
                db.Transactions.RemoveRange(transactions);
            }

            if (manuallyAddingFundTransactions.Count() > 0)
            {
                db.Transactions.RemoveRange(transactions);
            }

            db.SubscriptionBeneficiaries.RemoveRange(subscription.Beneficiaries);
            db.SubscriptionTypes.RemoveRange(subscription.Types);
            db.BudgetAllowances.RemoveRange(subscription.BudgetAllowances);
            db.Subscriptions.Remove(subscription);

            await db.SaveChangesAsync();
            logger.LogInformation($"Subscription deleted ({subscriptionId}, {subscription.Name})");
        }

        [MutationInput]
        public class Input : HaveSubscriptionId, IRequest {}

        public class SubscriptionNotFoundException : RequestValidationException { }
        public class CantDeleteSubscriptionWithBeneficiaries : RequestValidationException { }
    }
}
