using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.Gql.Bases;
using System.Collections.Generic;
using Sig.App.Backend.BackgroundJobs;

namespace Sig.App.Backend.Requests.Commands.Mutations.Subscriptions
{
    public class AssignBeneficiariesToSubscription : IRequestHandler<AssignBeneficiariesToSubscription.Input, AssignBeneficiariesToSubscription.Payload>
    {
        private readonly ILogger<AssignBeneficiariesToSubscription> logger;
        private IClock clock;
        private readonly AppDbContext db;
        private readonly ILogger<AddingFundToCard> addingFundLogger;

        public AssignBeneficiariesToSubscription(ILogger<AssignBeneficiariesToSubscription> logger, IClock clock, AppDbContext db, ILogger<AddingFundToCard> addingFundLogger)
        {
            this.logger = logger;
            this.clock = clock;
            this.db = db;
            this.addingFundLogger = addingFundLogger;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AssignBeneficiariesToSubscription({request.OrganizationId}, {request.SubscriptionId}, {request.Beneficiaries})");
            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations.Include(x => x.BudgetAllowances).FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null)
            {
                logger.LogWarning("[Mutation] AssignBeneficiariesToSubscription - OrganizationNotFoundException");
                throw new OrganizationNotFoundException();
            }

            var subscriptionId = request.SubscriptionId.LongIdentifierForType<Subscription>();
            var subscription = await db.Subscriptions.Include(x => x.Types).Include(x => x.Beneficiaries).FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

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
                .Where(x => beneficiariesLongIdentifiers.Contains(x.Id));

            AddingFundToCard addingFundToCardJob = null;
            if (request.ReplicatePaymentOnAttribution)
            {
                query = query.Include(x => x.Card);
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

            var paymentRemaining = subscription.GetPaymentRemaining(clock);

            if (subscription.IsSubscriptionPaymentBasedCardUsage)
            {
                paymentRemaining = Math.Min(subscription.MaxNumberOfPayments.Value, paymentRemaining);
            }

            decimal totalAmount = 0;
            var beneficiariesWhoGetSubscriptions = 0;

            logger.LogInformation($"[Mutation] AssignBeneficiariesToSubscription - Beneficiary count that fit the search ({beneficiaries.Length})");

            if (subscription.EndDate < today)
            {
                beneficiariesWhoGetSubscriptions = beneficiaries.Length;
                foreach (var beneficiary in beneficiaries)
                {
                    subscription.Beneficiaries.Add(new SubscriptionBeneficiary()
                    {
                        BeneficiaryId = beneficiary.Id,
                        SubscriptionId = subscriptionId,
                        BudgetAllowanceId = budgetAllowance.Id,
                        BeneficiaryType = beneficiary.BeneficiaryType
                    });

                    logger.LogInformation(
                        $"[Mutation] AssignBeneficiariesToSubscription - Beneficiary {beneficiary.Firstname} {beneficiary.Lastname} added to subscription {subscription.Name}");
                }
            }
            else
            {
                foreach (var beneficiary in beneficiaries)
                {
                    var amount =
                        subscription.Types.Where(x => x.BeneficiaryTypeId == beneficiary.BeneficiaryTypeId)
                            .Sum(x => x.Amount) * (request.ReplicatePaymentOnAttribution && beneficiary.Card != null ? paymentRemaining + 1 : paymentRemaining);

                    if (budgetAllowance.AvailableFund >= amount)
                    {
                        subscription.Beneficiaries.Add(new SubscriptionBeneficiary()
                        {
                            BeneficiaryId = beneficiary.Id,
                            SubscriptionId = subscriptionId,
                            BudgetAllowanceId = budgetAllowance.Id,
                            BeneficiaryType = beneficiary.BeneficiaryType
                        });

                        budgetAllowance.AvailableFund -= amount;
                        totalAmount += amount;
                        beneficiariesWhoGetSubscriptions++;

                        if (request.ReplicatePaymentOnAttribution && beneficiary.Card != null)
                        {
                            await addingFundToCardJob.AddFundToSpecificBeneficiary(beneficiary.GetIdentifier(), beneficiary.BeneficiaryType, subscription.GetIdentifier());
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
