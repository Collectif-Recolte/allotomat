using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Subscriptions
{
    public class ChangeBeneficiarySubscriptionMaxNumberOfPayments : IRequestHandler<ChangeBeneficiarySubscriptionMaxNumberOfPayments.Input, ChangeBeneficiarySubscriptionMaxNumberOfPayments.Payload>
    {
        private readonly ILogger<ChangeBeneficiarySubscriptionMaxNumberOfPayments> logger;
        private readonly AppDbContext db;

        public ChangeBeneficiarySubscriptionMaxNumberOfPayments(ILogger<ChangeBeneficiarySubscriptionMaxNumberOfPayments> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] ChangeBeneficiarySubscriptionMaxNumberOfPayments({request.BeneficiaryId}, {request.SubscriptionId}, {request.MaxNumberOfPayments})");

            var beneficiaryId = request.BeneficiaryId.LongIdentifierForType<Beneficiary>();
            var subscriptionId = request.SubscriptionId.LongIdentifierForType<Subscription>();

            var subscriptionBeneficiary = await db.SubscriptionBeneficiaries
                .Include(x => x.Subscription).ThenInclude(x => x.Types)
                .Include(x => x.BudgetAllowance)
                .Include(x => x.Beneficiary)
                .Where(x => x.BeneficiaryId == beneficiaryId && x.SubscriptionId == subscriptionId)
                .FirstOrDefaultAsync(cancellationToken);

            if (subscriptionBeneficiary == null)
            {
                logger.LogWarning("[Mutation] ChangeBeneficiarySubscriptionMaxNumberOfPayments - BeneficiaryNotInSubscriptionException");
                throw new BeneficiaryNotInSubscriptionException();
            }

            var subscription = subscriptionBeneficiary.Subscription;
            var currentMax = subscriptionBeneficiary.MaxNumberOfPaymentsOverride
                ?? subscription.MaxNumberOfPayments
                ?? subscriptionBeneficiary.GetTotalPayment();

            if (request.MaxNumberOfPayments <= currentMax)
            {
                logger.LogWarning("[Mutation] ChangeBeneficiarySubscriptionMaxNumberOfPayments - MaxNumberOfPaymentsMustBeGreaterThanCurrentException");
                throw new MaxNumberOfPaymentsMustBeGreaterThanCurrentException();
            }

            var additionalPayments = request.MaxNumberOfPayments - currentMax;
            var amountPerPayment = subscription.Types
                .Where(x => x.BeneficiaryTypeId == subscriptionBeneficiary.BeneficiaryTypeId)
                .Sum(x => x.Amount);
            var totalCost = additionalPayments * amountPerPayment;

            if (subscriptionBeneficiary.BudgetAllowance.AvailableFund < totalCost)
            {
                logger.LogWarning("[Mutation] ChangeBeneficiarySubscriptionMaxNumberOfPayments - NotEnoughBudgetAllowanceException");
                throw new NotEnoughBudgetAllowanceException();
            }

            subscriptionBeneficiary.BudgetAllowance.AvailableFund -= totalCost;
            subscriptionBeneficiary.MaxNumberOfPaymentsOverride = request.MaxNumberOfPayments;

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] ChangeBeneficiarySubscriptionMaxNumberOfPayments - MaxNumberOfPaymentsOverride set to {request.MaxNumberOfPayments} for beneficiary {beneficiaryId} in subscription {subscriptionId}");

            return new Payload { Beneficiary = new BeneficiaryGraphType(subscriptionBeneficiary.Beneficiary) };
        }

        [MutationInput]
        public class Input : HaveSubscriptionIdAndBeneficiaryId, IRequest<Payload>
        {
            public int MaxNumberOfPayments { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public BeneficiaryGraphType Beneficiary { get; set; }
        }

        public class BeneficiaryNotInSubscriptionException : RequestValidationException { }
        public class MaxNumberOfPaymentsMustBeGreaterThanCurrentException : RequestValidationException { }
        public class NotEnoughBudgetAllowanceException : RequestValidationException { }
    }
}
