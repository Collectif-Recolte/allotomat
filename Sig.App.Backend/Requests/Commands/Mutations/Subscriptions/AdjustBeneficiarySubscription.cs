using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Subscriptions
{
    public class AdjustBeneficiarySubscription : IRequestHandler<AdjustBeneficiarySubscription.Input, AdjustBeneficiarySubscription.Payload>
    {
        private readonly ILogger<AdjustBeneficiarySubscription> logger;
        private readonly IClock clock;
        private readonly AppDbContext db;

        public AdjustBeneficiarySubscription(ILogger<AdjustBeneficiarySubscription> logger, IClock clock, AppDbContext db)
        {
            this.logger = logger;
            this.clock = clock;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AdjustBeneficiarySubscription({request.BeneficiaryId}, {request.SubscriptionIds})");
            var subscriptionIds = request.SubscriptionIds.Select(x => x.LongIdentifierForType<Subscription>());
            var beneficiaryId = request.BeneficiaryId.LongIdentifierForType<Beneficiary>();

            var beneficiaryTransactions = await db.Transactions.OfType<SubscriptionAddingFundTransaction>().Where(x => x.BeneficiaryId == beneficiaryId && subscriptionIds.Contains(x.SubscriptionType.SubscriptionId)).ToListAsync();

            var beneficiary = await db.Beneficiaries
                .Include(x => x.Subscriptions).ThenInclude(x => x.Subscription).ThenInclude(x => x.Types)
                .Include(x => x.Subscriptions).ThenInclude(x => x.BudgetAllowance)
                .FirstOrDefaultAsync(x => x.Id == beneficiaryId, cancellationToken);

            if (beneficiary == null)
            {
                logger.LogWarning("[Mutation] AdjustBeneficiarySubscription - BeneficiaryNotFoundException");
                throw new BeneficiaryNotFoundException();
            }

            foreach (var subscriptionId in subscriptionIds)
            {
                var subscriptionBeneficiary = beneficiary.Subscriptions.Where(x => x.SubscriptionId == subscriptionId).FirstOrDefault();

                if (subscriptionBeneficiary == null)
                {
                    logger.LogWarning("[Mutation] AdjustBeneficiarySubscription - SubscriptionNotFoundException");
                    throw new SubscriptionNotFoundException();
                }

                var previousPaymentAmount = GetAmountPayment(subscriptionBeneficiary.Subscription, subscriptionBeneficiary.BeneficiaryTypeId.Value);
                var newPaymentAmount = GetAmountPayment(subscriptionBeneficiary.Subscription, beneficiary.BeneficiaryTypeId.Value);

                var paymentReceived = beneficiaryTransactions.Where(x => x.SubscriptionType.SubscriptionId == subscriptionBeneficiary.SubscriptionId).Count();
                var paymentRemaining = subscriptionBeneficiary.Subscription.GetPaymentRemaining(clock);
                var numberOfPaymentToReceive = Math.Min(subscriptionBeneficiary.Subscription.MaxNumberOfPayments.HasValue ? subscriptionBeneficiary.Subscription.MaxNumberOfPayments.Value - paymentReceived : paymentRemaining, paymentRemaining);

                if (subscriptionBeneficiary.BudgetAllowance.AvailableFund + (previousPaymentAmount - newPaymentAmount) * numberOfPaymentToReceive > 0)
                {
                    subscriptionBeneficiary.BudgetAllowance.AvailableFund += (previousPaymentAmount - newPaymentAmount) * numberOfPaymentToReceive;
                    subscriptionBeneficiary.BeneficiaryTypeId = beneficiary.BeneficiaryTypeId.Value;
                }
                else
                {
                    throw new NotEnoughBudgetAllowanceException();
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            return new Payload() { Beneficiary = new BeneficiaryGraphType(beneficiary) };
        }

        private decimal GetAmountPayment(Subscription subscription, long beneficiaryTypeId) 
        {
            decimal amount = 0;
            
            var types = subscription.Types;
            foreach (var type in types)
            {
                if (type.BeneficiaryTypeId == beneficiaryTypeId)
                {
                    amount += type.Amount;
                }
            }

            return amount;
        }

        [MutationInput]
        public class Input : IRequest<Payload>
        {
            public Id BeneficiaryId { get; set; }
            public IEnumerable<Id> SubscriptionIds { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public BeneficiaryGraphType Beneficiary { get; set; }
        }

        public class BeneficiaryNotFoundException : RequestValidationException { }
        public class SubscriptionNotFoundException : RequestValidationException { }
        public class NotEnoughBudgetAllowanceException : RequestValidationException { }
    }
}
