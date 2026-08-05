using GraphQL.Conventions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.BackgroundJobs;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
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
    public class AssignBeneficiariesToSubscription : IRequestHandler<AssignBeneficiariesToSubscription.Input, AssignBeneficiariesToSubscription.Payload>
    {
        private readonly ILogger<AssignBeneficiariesToSubscription> logger;
        private IClock clock;
        private readonly AppDbContext db;
        private readonly ILogger<AddingFundToCard> addingFundLogger;
        private readonly IHttpContextAccessor httpContextAccessor;

        public AssignBeneficiariesToSubscription(ILogger<AssignBeneficiariesToSubscription> logger, IClock clock, IHttpContextAccessor httpContextAccessor, AppDbContext db, ILogger<AddingFundToCard> addingFundLogger)
        {
            this.logger = logger;
            this.clock = clock;
            this.db = db;
            this.addingFundLogger = addingFundLogger;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AssignBeneficiariesToSubscription({request.OrganizationId}, {request.SubscriptionId}, {request.Beneficiaries})");
            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations
                .Include(x => x.BudgetAllowances)
                .Include(x => x.Project)
                .FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null)
            {
                logger.LogWarning("[Mutation] AssignBeneficiariesToSubscription - OrganizationNotFoundException");
                throw new OrganizationNotFoundException();
            }

            var subscriptionId = request.SubscriptionId.LongIdentifierForType<Subscription>();
            var subscription = await db.Subscriptions.Include(x => x.Types).ThenInclude(x => x.ProductGroup).Include(x => x.Beneficiaries).FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

            if (subscription == null)
            {
                logger.LogWarning("[Mutation] AssignBeneficiariesToSubscription - SubscriptionNotFoundException");
                throw new SubscriptionNotFoundException();
            }

            var beneficiariesLongIdentifiers = request.Beneficiaries.Select(x => x.LongIdentifierForType<Beneficiary>());
            if (subscription.Beneficiaries.Select(x => x.BeneficiaryId).Intersect(beneficiariesLongIdentifiers).Any())
            {
                logger.LogWarning("[Mutation] AssignBeneficiariesToSubscription - BeneficiaryAlreadyGotSubscriptionException");
                throw new BeneficiaryAlreadyGotSubscriptionException();
            }

            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            if (subscription.GetLastDateToAssignBeneficiary() < today)
            {
                logger.LogWarning("[Mutation] AssignBeneficiariesToSubscription - SubscriptionAlreadyExpiredException");
                throw new SubscriptionAlreadyExpiredException();
            }

            var budgetAllowance = organization.BudgetAllowances.FirstOrDefault(x => x.SubscriptionId == subscriptionId);

            if (budgetAllowance == null)
            {
                logger.LogWarning("[Mutation] AssignBeneficiariesToSubscription - MissingBudgetAllowanceException");
                throw new MissingBudgetAllowanceException();
            }

            IQueryable<Beneficiary> query = db.Beneficiaries
                .Include(x => x.BeneficiaryType)
                .Include(x => x.Card)
                .Where(x => beneficiariesLongIdentifiers.Contains(x.Id));

            AddingFundToCard addingFundToCardJob = null;
            string currentUserId = null;
            AppUser currentUser = null;
            currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            currentUser = currentUserId != null
                ? db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId)
                : null;
            if (request.ReplicatePaymentOnAttribution)
            {
                query = query
                    .Include(x => x.Card).ThenInclude(x => x.Transactions)
                    .Include(x => x.Card).ThenInclude(x => x.Funds)
                    .Include(x => x.Organization).ThenInclude(x => x.Project);
                addingFundToCardJob = new AddingFundToCard(db, clock, addingFundLogger);
            }

            Beneficiary[] beneficiaries = query.ToArray();

            if (beneficiaries.Length != beneficiariesLongIdentifiers.Count())
            {
                logger.LogWarning("[Mutation] AssignBeneficiariesToSubscription - BeneficiaryNotFoundException");
                throw new BeneficiaryNotFoundException();
            }

            var beneficiariesType = beneficiaries.Select(x => x.BeneficiaryTypeId).Distinct();
            if (subscription.Types.Select(x => x.BeneficiaryTypeId).Intersect(beneficiariesType).Count() != beneficiariesType.Count())
            {
                logger.LogWarning("[Mutation] AssignBeneficiariesToSubscription - BeneficiaryTypeNotInSubscriptionException");
                throw new BeneficiaryTypeNotInSubscriptionException();
            }

            var paymentRemaining = await subscription.GetPaymentRemainingAsync(db, clock, cancellationToken);
            var calendarRemaining = await subscription.GetCardPaymentRemainingAsync(db, clock, cancellationToken);

            if (subscription.IsSubscriptionPaymentBasedCardUsage)
            {
                paymentRemaining = Math.Min(subscription.MaxNumberOfPayments.Value, paymentRemaining);
            }

            var beneficiariesWhoGetSubscriptions = 0;

            logger.LogInformation($"[Mutation] AssignBeneficiariesToSubscription - Beneficiary count that fit the search ({beneficiaries.Length})");

            if (subscription.EndDate < today)
            {
                beneficiariesWhoGetSubscriptions = beneficiaries.Length;
                foreach (var beneficiary in beneficiaries)
                {
                    var subscriptionBeneficiary = new SubscriptionBeneficiary()
                    {
                        BeneficiaryId = beneficiary.Id,
                        SubscriptionId = subscriptionId,
                        BudgetAllowanceId = budgetAllowance.Id,
                        BeneficiaryType = beneficiary.BeneficiaryType,
                        Beneficiary = beneficiary,
                        Subscription = subscription,
                        RemainingAllocatedAmount = 0m
                    };
                    subscription.Beneficiaries.Add(subscriptionBeneficiary);

                    if (request.ReplicatePaymentOnAttribution && beneficiary.Card != null)
                    {
                        var maxPayments = subscription.MaxNumberOfPayments ?? subscription.GetTotalPayment();
                        var shouldReplicate = request.ReplicatePaymentOnAttribution && beneficiary.Card != null;

                        var beneficiaryPaymentRemaining = shouldReplicate
                            ? Math.Min(maxPayments, paymentRemaining + 1)
                            : paymentRemaining;

                        var amount = subscription.Types.Where(x => x.BeneficiaryTypeId == beneficiary.BeneficiaryTypeId).Sum(x => x.Amount) * beneficiaryPaymentRemaining;

                        if (budgetAllowance.AvailableFund >= amount)
                        {
                            budgetAllowance.AvailableFund -= amount;
                            subscriptionBeneficiary.RemainingAllocatedAmount += amount;

                            if (amount > 0)
                            {
                                AddAllocationTransactionLog(beneficiary, organization, subscription, beneficiaryPaymentRemaining, amount, today, currentUserId, currentUser);
                            }
                            await addingFundToCardJob.AddFundToExistingSubscriptionBeneficiary(subscriptionBeneficiary, new AddingFundToCard.InitiatedBy()
                            {
                                TransactionInitiatorId = currentUserId,
                                TransactionInitiatorEmail = currentUser?.Email,
                                TransactionInitiatorFirstname = currentUser?.Profile.FirstName,
                                TransactionInitiatorLastname = currentUser?.Profile.LastName
                            });
                        }
                    }

                    logger.LogInformation(
                        $"[Mutation] AssignBeneficiariesToSubscription - Beneficiary {beneficiary.Firstname} {beneficiary.Lastname} added to subscription {subscription.Name}");
                }
            }
            else
            {
                foreach (var beneficiary in beneficiaries)
                {
                    var beneficiaryPaymentsMade = 0;
                    var beneficiaryPaymentRemaining = paymentRemaining;

                    if (subscription.IsSubscriptionPaymentBasedCardUsage)
                    {
                        var rawTransactionCount = await db.Transactions
                            .Include(x => (x as SubscriptionAddingFundTransaction).SubscriptionType)
                            .OfType<SubscriptionAddingFundTransaction>()
                            .Where(x => x.BeneficiaryId == beneficiary.Id && x.SubscriptionType.SubscriptionId == subscription.Id)
                            .CountAsync(cancellationToken);

                        var numberOfPaymentTypes = subscription.GetNumberOfPaymentTypes(beneficiary.BeneficiaryTypeId);
                        beneficiaryPaymentsMade = SubscriptionHelper.GetNumberOfPaymentsMade(rawTransactionCount, numberOfPaymentTypes);

                        // Le quota est borné par le calendrier, pas l'inverse. Soustraire les versements
                        // livrés d'un calendrier déjà plafonné au quota en retirerait un de trop en fin
                        // d'abonnement, quand il reste moins de dates de versement que de quota.
                        beneficiaryPaymentRemaining = Math.Max(0,
                            Math.Min(calendarRemaining, subscription.MaxNumberOfPayments.Value - beneficiaryPaymentsMade));
                    }

                    var amountPerPayment = subscription.Types
                        .Where(x => x.BeneficiaryTypeId == beneficiary.BeneficiaryTypeId)
                        .Sum(x => x.Amount);

                    var replicatePaymentOnAttribution = request.ReplicatePaymentOnAttribution && beneficiary.Card != null && subscription.GetTotalPayment() - beneficiaryPaymentsMade > 0;

                    var numberOfPayments = replicatePaymentOnAttribution
                        ? Math.Min(subscription.GetTotalPayment() - beneficiaryPaymentsMade, beneficiaryPaymentRemaining + 1)
                        : beneficiaryPaymentRemaining;

                    var amount = amountPerPayment * numberOfPayments;

                    if (budgetAllowance.AvailableFund >= amount)
                    {
                        var subscriptionBeneficiary = new SubscriptionBeneficiary()
                        {
                            BeneficiaryId = beneficiary.Id,
                            SubscriptionId = subscriptionId,
                            BudgetAllowanceId = budgetAllowance.Id,
                            BeneficiaryType = beneficiary.BeneficiaryType,
                            Beneficiary = beneficiary,
                            Subscription = subscription,
                            RemainingAllocatedAmount = amount
                        };
                        subscription.Beneficiaries.Add(subscriptionBeneficiary);

                        budgetAllowance.AvailableFund -= amount;
                        beneficiariesWhoGetSubscriptions++;

                        if (amount > 0)
                        {
                            AddAllocationTransactionLog(beneficiary, organization, subscription, numberOfPayments, amount, today, currentUserId, currentUser);
                        }

                        if (replicatePaymentOnAttribution)
                        {
                            await addingFundToCardJob.AddFundToExistingSubscriptionBeneficiary(subscriptionBeneficiary, new AddingFundToCard.InitiatedBy()
                            {
                                TransactionInitiatorId = currentUserId,
                                TransactionInitiatorEmail = currentUser?.Email,
                                TransactionInitiatorFirstname = currentUser?.Profile.FirstName,
                                TransactionInitiatorLastname = currentUser?.Profile.LastName
                            });
                        }

                        logger.LogInformation(
                            $"[Mutation] AssignBeneficiariesToSubscription - Beneficiary {beneficiary.Firstname} {beneficiary.Lastname} added to subscription {subscription.Name}");
                    }
                    else
                    {
                        logger.LogWarning("[Mutation] AssignBeneficiariesToSubscription - NotEnoughBudgetAllowanceException");
                        throw new NotEnoughBudgetAllowanceException();
                    }
                }
            }

            logger.LogInformation($"[Mutation] AssignBeneficiariesToSubscription - Beneficiary who get a subscriptions ({beneficiariesWhoGetSubscriptions})");

            await db.SaveChangesAsync(cancellationToken);

            return new Payload()
            {
                Organization = new OrganizationGraphType(organization),
                BeneficiariesWhoGetSubscriptions = beneficiariesWhoGetSubscriptions,
                TotalBeneficiaries = beneficiaries.Length,
                AvailableBudgetAfter = budgetAllowance.AvailableFund
            };
        }

        private void AddAllocationTransactionLog(
            Beneficiary beneficiary,
            Organization organization,
            Subscription subscription,
            int numberOfPayments,
            decimal amount,
            DateTime today,
            string currentUserId,
            AppUser currentUser)
        {
            var subscriptionTypes = subscription.Types.Where(x => x.BeneficiaryTypeId == beneficiary.BeneficiaryTypeId).ToList();
            var transactionLogProductGroups = new List<TransactionLogProductGroup>();
            foreach (var productGroup in subscriptionTypes.GroupBy(x => x.ProductGroupId))
            {
                var firstType = productGroup.First();
                transactionLogProductGroups.Add(new TransactionLogProductGroup()
                {
                    Amount = numberOfPayments * productGroup.Sum(x => x.Amount),
                    ProductGroupId = firstType.ProductGroupId,
                    ProductGroupName = firstType.ProductGroup?.Name
                });
            }

            db.TransactionLogs.Add(new TransactionLog()
            {
                Discriminator = TransactionLogDiscriminator.AllocateBudgetAllowanceFromSubscriptionAssignmentTransactionLog,
                CreatedAtUtc = today,
                TotalAmount = amount,
                CardProgramCardId = beneficiary.Card?.ProgramCardId,
                CardNumber = beneficiary.Card?.CardNumber,
                BeneficiaryId = beneficiary.Id,
                BeneficiaryID1 = beneficiary.ID1,
                BeneficiaryID2 = beneficiary.ID2,
                BeneficiaryFirstname = beneficiary.Firstname,
                BeneficiaryLastname = beneficiary.Lastname,
                BeneficiaryEmail = beneficiary.Email,
                BeneficiaryPhone = beneficiary.Phone,
                BeneficiaryIsOffPlatform = beneficiary is OffPlatformBeneficiary,
                BeneficiaryTypeId = beneficiary.BeneficiaryTypeId,
                OrganizationId = organization.Id,
                OrganizationName = organization.Name,
                SubscriptionId = subscription.Id,
                SubscriptionName = subscription.Name,
                ProjectId = organization.ProjectId,
                ProjectName = organization.Project?.Name,
                TransactionLogProductGroups = transactionLogProductGroups,
                TransactionInitiatorId = currentUserId,
                TransactionInitiatorFirstname = currentUser?.Profile.FirstName,
                TransactionInitiatorLastname = currentUser?.Profile.LastName,
                TransactionInitiatorEmail = currentUser?.Email
            });
        }

        [MutationInput]
        public class Input : HaveOrganizationIdAndSubscriptionId, IRequest<Payload>
        {
            public IEnumerable<Id> Beneficiaries { get; set; }
            public bool ReplicatePaymentOnAttribution { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public OrganizationGraphType Organization { get; set; }
            public int BeneficiariesWhoGetSubscriptions { get; set; }
            public int TotalBeneficiaries { get; set; }
            public decimal AvailableBudgetAfter { get; set; }
        }

        public class OrganizationNotFoundException : RequestValidationException { }
        public class SubscriptionNotFoundException : RequestValidationException { }
        public class SubscriptionAlreadyExpiredException : RequestValidationException { }
        public class MissingBudgetAllowanceException : RequestValidationException { }
        public class BeneficiaryNotFoundException : RequestValidationException { }
        public class BeneficiaryAlreadyGotSubscriptionException : RequestValidationException { }
        public class BeneficiaryTypeNotInSubscriptionException : RequestValidationException { }
        public class NotEnoughBudgetAllowanceException : RequestValidationException { }

        public enum AttributionSort
        {
            Default,
            Random
        }
    }
}
