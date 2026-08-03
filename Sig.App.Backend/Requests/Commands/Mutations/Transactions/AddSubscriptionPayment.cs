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
    public class AddSubscriptionPayment : IRequestHandler<AddSubscriptionPayment.Input, AddSubscriptionPayment.Payload>
    {
        private readonly ILogger<AddSubscriptionPayment> logger;
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<AddingFundToCard> addingFundLogger;

        public AddSubscriptionPayment(ILogger<AddSubscriptionPayment> logger, AppDbContext db, IClock clock, IHttpContextAccessor httpContextAccessor, ILogger<AddingFundToCard> addingFundLogger)
        {
            this.logger = logger;
            this.db = db;
            this.clock = clock;
            this.httpContextAccessor = httpContextAccessor;
            this.addingFundLogger = addingFundLogger;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AddSubscriptionPayment({request.SubscriptionId}, {request.BeneficiaryId})");
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
                logger.LogWarning("[Mutation] AddSubscriptionPayment - BeneficiaryNotFoundException");
                throw new BeneficiaryNotFoundException();
            }

            if (beneficiary.Card == null)
            {
                logger.LogWarning("[Mutation] AddSubscriptionPayment - BeneficiaryDontHaveCardException");
                throw new BeneficiaryDontHaveCardException();
            }

            var subscriptionId = request.SubscriptionId.LongIdentifierForType<Subscription>();

            if (!db.Subscriptions.Where(x => x.Id == subscriptionId).Any())
            {
                logger.LogWarning("[Mutation] AddSubscriptionPayment - SubscriptionNotFoundException");
                throw new SubscriptionNotFoundException();
            }

            var subscriptionBeneficiary = beneficiary.Subscriptions.FirstOrDefault(x => x.SubscriptionId == subscriptionId);

            if (subscriptionBeneficiary == null)
            {
                logger.LogWarning("[Mutation] AddSubscriptionPayment - BeneficiaryDontHaveThisSubscriptionException");
                throw new BeneficiaryDontHaveThisSubscriptionException();
            }

            var subscription = subscriptionBeneficiary.Subscription;

            if (subscription.IsExpired(clock))
            {
                logger.LogWarning("[Mutation] AddSubscriptionPayment - SubscriptionExpiredException");
                throw new SubscriptionExpiredException();
            }

            if (!subscription.HasSubscriptionPaymentPeriodStarted(clock))
            {
                logger.LogWarning("[Mutation] AddSubscriptionPayment - SubscriptionMaxPaymentsReachedException");
                throw new SubscriptionMaxPaymentsReachedException();
            }

            var amount = subscription.Types.Where(x => x.BeneficiaryTypeId == beneficiary.BeneficiaryTypeId).Sum(x => x.Amount);

            if (subscriptionBeneficiary.BudgetAllowance.AvailableFund < amount)
            {
                logger.LogWarning("[Mutation] AddSubscriptionPayment - SubscriptionDontHaveEnoughtAvailableAmountException");
                throw new SubscriptionDontHaveEnoughtAvailableAmountException();
            }

            var transactions = await db.Transactions.OfType<SubscriptionAddingFundTransaction>()
                .Where(x => x.Status != DbModel.Enums.FundTransactionStatus.Unassigned)
                .Include(x => x.SubscriptionType)
                .Where(x => x.BeneficiaryId == beneficiary.Id && x.SubscriptionType.SubscriptionId == subscription.Id).ToListAsync();

            var subscriptionPaymentRemaining = await subscriptionBeneficiary.GetPaymentRemainingAsync(db, clock, cancellationToken);

            var numberOfPaymentTypes = subscription.GetNumberOfPaymentTypes(beneficiary.BeneficiaryTypeId);
            var paymentsMade = SubscriptionHelper.GetNumberOfPaymentsMade(transactions.Count, numberOfPaymentTypes);

            // Aucune limite si aucun max explicite (override ou Subscription.MaxNumberOfPayments) n'est défini.
            var explicitMax = subscriptionBeneficiary.GetExplicitMaxNumberOfPayments();
            if (explicitMax.HasValue && paymentsMade >= explicitMax.Value)
            {
                logger.LogWarning("[Mutation] AddSubscriptionPayment - SubscriptionMaxPaymentsReachedException");
                throw new SubscriptionMaxPaymentsReachedException();
            }

            var maxNumberOfPayments = subscriptionBeneficiary.GetEffectiveMaxNumberOfPayments();
            // Un versement au-delà du calendrier réservé (paymentsMade >= max effectif) débite l'enveloppe.
            // CRCL-2603 : le saut de débit n'est valable que pour les abonnements usage-based (un versement peut
            // être réservé sans être livré). Pour un abonnement non usage-based, le job livre chaque versement
            // programmé quoi qu'il arrive : un versement manqué est toujours additionnel et doit toujours débiter.
            var isBudgetAllowanceAlreadyAllocated = subscription.IsSubscriptionPaymentBasedCardUsage
                && paymentsMade < maxNumberOfPayments
                && maxNumberOfPayments - paymentsMade <= Math.Min(maxNumberOfPayments - paymentsMade, subscriptionPaymentRemaining);
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
                Beneficiary = new BeneficiaryGraphType(beneficiary, beneficiary.Organization?.Project?.BeneficiariesAreAnonymous ?? true)
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
        public class SubscriptionMaxPaymentsReachedException : RequestValidationException { }
    }
}
