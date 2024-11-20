using MediatR;
using Sig.App.Backend.Plugins.GraphQL;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using NodaTime;
using Sig.App.Backend.DbModel;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Extensions;
using System.Linq;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using GraphQL.Conventions;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.BackgroundJobs;
using Sig.App.Backend.DbModel.Entities.Transactions;
using System;

namespace Sig.App.Backend.Requests.Commands.Mutations.Transactions
{
    public class AddMissingPayment : IRequestHandler<AddMissingPayment.Input, AddMissingPayment.Payload>
    {
        private readonly ILogger<AddMissingPayment> logger;
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<AddingFundToCard> addingFundLogger;

        public AddMissingPayment(ILogger<AddMissingPayment> logger, AppDbContext db, IClock clock, IHttpContextAccessor httpContextAccessor, ILogger<AddingFundToCard> addingFundLogger)
        {
            this.logger = logger;
            this.db = db;
            this.clock = clock;
            this.httpContextAccessor = httpContextAccessor;
            this.addingFundLogger = addingFundLogger;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AddMissingPayment({request.SubscriptionId}, {request.BeneficiaryId})");
            var today = clock
                .GetCurrentInstant()
                .InUtc()
                .ToDateTimeUtc();
            var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            var currentUser = db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId);

            long beneficiaryId = request.BeneficiaryId.LongIdentifierForType<Beneficiary>();

            var beneficiary = await db.Beneficiaries
                .Include(x => x.BeneficiaryType)
                .Include(x => x.Card).ThenInclude(x => x.Transactions)
                .Include(x => x.Card).ThenInclude(x => x.Funds)
                .Include(x => x.Organization).ThenInclude(x => x.Project)
                .Include(x => x.Subscriptions).ThenInclude(x => x.Subscription).ThenInclude(x => x.Types)
                .Include(x => x.Subscriptions).ThenInclude(x => x.BudgetAllowance)
                .FirstOrDefaultAsync(x => x.Id == beneficiaryId, cancellationToken);

            if (beneficiary == null)
            {
                logger.LogWarning("[Mutation] AddMissingPayment - BeneficiaryNotFoundException");
                throw new BeneficiaryNotFoundException();
            }

            if (beneficiary.Card == null)
            {
                logger.LogWarning("[Mutation] AddMissingPayment - BeneficiaryDontHaveCardException");
                throw new BeneficiaryDontHaveCardException();
            }

            var subscriptionId = request.SubscriptionId.LongIdentifierForType<Subscription>();

            if (!db.Subscriptions.Where(x => x.Id == subscriptionId).Any())
            {
                logger.LogWarning("[Mutation] AddMissingPayment - SubscriptionNotFoundException");
                throw new SubscriptionNotFoundException();
            }

            var subscriptionBeneficiary = beneficiary.Subscriptions.FirstOrDefault(x => x.SubscriptionId == subscriptionId);

            if (subscriptionBeneficiary == null)
            {
                logger.LogWarning("[Mutation] AddMissingPayment - BeneficiaryDontHaveThisSubscriptionException");
                throw new BeneficiaryDontHaveThisSubscriptionException();
            }

            var subscription = subscriptionBeneficiary.Subscription;

            if (subscription.GetExpirationDate(clock) < today)
            {
                logger.LogWarning("[Mutation] AddMissingPayment - SubscriptionExpiredException");
                throw new SubscriptionExpiredException();
            }

            var amount = subscription.Types.Where(x => x.BeneficiaryTypeId == beneficiary.BeneficiaryTypeId).Sum(x => x.Amount);

            if (subscriptionBeneficiary.BudgetAllowance.AvailableFund < amount)
            {
                logger.LogWarning("[Mutation] AddMissingPayment - SubscriptionDontHaveEnoughtAvailableAmountException");
                throw new SubscriptionDontHaveEnoughtAvailableAmountException();
            }

            var transactions = await db.Transactions.OfType<SubscriptionAddingFundTransaction>()
                .Include(x => x.SubscriptionType)
                .Where(x => x.BeneficiaryId == beneficiary.Id && x.SubscriptionType.SubscriptionId == subscription.Id).ToListAsync();

            var subscriptionTotalPayment = subscription.GetTotalPayment();
            var subscriptionPaymentRemaining = subscription.GetPaymentRemaining(clock);

            if (transactions.Count() >= subscriptionTotalPayment - subscriptionPaymentRemaining || (subscription.MaxNumberOfPayments.HasValue && transactions.Count() == subscription.MaxNumberOfPayments))
            {
                logger.LogWarning("[Mutation] AddMissingPayment - SubscriptionDontHaveMissedPaymentException");
                throw new SubscriptionDontHaveMissedPaymentException();
            }

            var maxNumberOfPayments = subscription.MaxNumberOfPayments.HasValue ? subscription.MaxNumberOfPayments.Value : subscriptionTotalPayment;
            var isBudgetAllowanceAlreadyAllocated = maxNumberOfPayments - transactions.Count <= Math.Min(maxNumberOfPayments - transactions.Count(), subscriptionPaymentRemaining);
            if (!isBudgetAllowanceAlreadyAllocated)
            {
                subscriptionBeneficiary.BudgetAllowance.AvailableFund -= amount;
            }

            var addingFundToCardJob = new AddingFundToCard(db, clock, addingFundLogger);
            await addingFundToCardJob.AddFundToSpecificBeneficiary(beneficiary.GetIdentifier(), beneficiary.BeneficiaryType, subscription.GetIdentifier(), new AddingFundToCard.InitiatedBy()
            {
                TransactionInitiatorId = currentUserId,
                TransactionInitiatorEmail = currentUser?.Email,
                TransactionInitiatorFirstname = currentUser?.Profile.FirstName,
                TransactionInitiatorLastname = currentUser?.Profile.LastName
            });

            await db.SaveChangesAsync(cancellationToken);

            return new Payload()
            {
                Beneficiary = new BeneficiaryGraphType(beneficiary)
            };
        }

        [MutationInput]
        public class Input : IRequest<Payload>
        {
            public Id BeneficiaryId { get; set; }
            public Id SubscriptionId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public BeneficiaryGraphType Beneficiary { get; set; }
        }

        public class BeneficiaryNotFoundException : RequestValidationException { }
        public class BeneficiaryDontHaveCardException : RequestValidationException { }
        public class BeneficiaryDontHaveThisSubscriptionException : RequestValidationException { }
        public class SubscriptionNotFoundException : RequestValidationException { }
        public class SubscriptionExpiredException : RequestValidationException { }
        public class SubscriptionDontHaveEnoughtAvailableAmountException : RequestValidationException { }
        public class SubscriptionDontHaveMissedPaymentException : RequestValidationException { }
    }
}
