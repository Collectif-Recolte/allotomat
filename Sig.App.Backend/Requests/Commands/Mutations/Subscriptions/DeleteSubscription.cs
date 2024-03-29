﻿using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Plugins.GraphQL;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Gql.Interfaces;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.DbModel.Entities.Subscriptions;

namespace Sig.App.Backend.Requests.Commands.Mutations.Subscriptions
{
    public class DeleteSubscription : AsyncRequestHandler<DeleteSubscription.Input>
    {
        private readonly ILogger<DeleteSubscription> logger;
        private readonly AppDbContext db;

        public DeleteSubscription(ILogger<DeleteSubscription> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        protected override async Task Handle(Input request, CancellationToken cancellationToken)
        {
            var subscriptionId = request.SubscriptionId.LongIdentifierForType<Subscription>();
            var subscription = await db.Subscriptions.Include(x => x.Types).Include(x => x.Beneficiaries).Include(x=> x.BudgetAllowances).FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

            if (subscription == null) throw new SubscriptionNotFoundException();

            if (db.SubscriptionBeneficiaries.Where(x => x.SubscriptionId == subscriptionId).Any()) throw new CantDeleteSubscriptionWithBeneficiaries();

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
        public class Input : IRequest, IHaveSubscriptionId
        {
            public Id SubscriptionId { get; set; }
        }

        public class SubscriptionNotFoundException : RequestValidationException { }
        public class CantDeleteSubscriptionWithBeneficiaries : RequestValidationException { }
    }
}
