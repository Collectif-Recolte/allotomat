using System.Collections.Generic;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.DbModel.Entities.Transactions;
using System;

namespace Sig.App.Backend.Requests.Commands.Mutations.Subscriptions
{
    public class RemoveBeneficiaryFromSubscription : IRequestHandler<RemoveBeneficiaryFromSubscription.Input>
    {
        private readonly ILogger<RemoveBeneficiaryFromSubscription> logger;
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly IHttpContextAccessor httpContextAccessor;

        public RemoveBeneficiaryFromSubscription(ILogger<RemoveBeneficiaryFromSubscription> logger, AppDbContext db, IClock clock, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.db = db;
            this.clock = clock;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] RemoveBeneficiaryFromSubscription({request.BeneficiaryId}, {request.SubscriptionId})");
            var subscriptionId = request.SubscriptionId.LongIdentifierForType<Subscription>();
            var subscription = await db.Subscriptions.Include(x => x.Types).ThenInclude(x => x.ProductGroup)
                .Include(x => x.Beneficiaries).ThenInclude(x => x.BudgetAllowance)
                .Include(x => x.Beneficiaries).ThenInclude(x => x.Beneficiary)
                .Include(x => x.Beneficiaries).ThenInclude(x => x.BeneficiaryType)
                .FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

            if (subscription == null)
            {
                logger.LogWarning("[Mutation] RemoveBeneficiaryFromSubscription - SubscriptionNotFoundException");
                throw new SubscriptionNotFoundException();
            }

            var beneficiaryId = request.BeneficiaryId.LongIdentifierForType<Beneficiary>();
            var beneficiary = await db.Beneficiaries
                .Include(x => x.Organization).ThenInclude(x => x.Project)
                .Include(x => x.Card)
                .FirstOrDefaultAsync(x => x.Id == beneficiaryId, cancellationToken);
            var beneficiaryTransactions = await db.Transactions
                .Include(x => (x as SubscriptionAddingFundTransaction).SubscriptionType)
                .Where(x => x.BeneficiaryId == beneficiaryId)
                .ToListAsync(cancellationToken);

            if (beneficiary == null)
            {
                logger.LogWarning("[Mutation] RemoveBeneficiaryFromSubscription - BeneficiaryNotFoundException");
                throw new BeneficiaryNotFoundException();
            }

            var subscriptionBeneficiary = subscription.Beneficiaries.FirstOrDefault(x => x.BeneficiaryId == beneficiaryId);
            if (subscriptionBeneficiary == null)
            {
                logger.LogWarning("[Mutation] RemoveBeneficiaryFromSubscription - BeneficiaryNotInSubscriptionException");
                throw new BeneficiaryNotInSubscriptionException();
            }
            
            var today = clock.GetCurrentInstant().ToDateTimeUtc();
            var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            var currentUser = db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId);

            beneficiary.Subscriptions.Remove(subscriptionBeneficiary);

            var subscriptionTypes = subscription.Types.Where(x => x.BeneficiaryTypeId == subscriptionBeneficiary.BeneficiaryTypeId).ToList();
            var amountPerPayment = subscriptionTypes.Sum(x => x.Amount);

            decimal totalRefund;
            if (subscriptionBeneficiary.RemainingAllocatedAmount.HasValue)
            {
                var remainingAllocated = subscriptionBeneficiary.RemainingAllocatedAmount.Value;

                if (remainingAllocated < 0)
                {
                    // Sur-livraison par rapport à la réservation : un retrait ne débite jamais l'enveloppe.
                    logger.LogWarning($"[Mutation] RemoveBeneficiaryFromSubscription - RemainingAllocatedAmount négatif ({remainingAllocated}) pour bénéficiaire {beneficiaryId} / abonnement {subscription.Id}; remboursement plafonné à 0.");
                }

                totalRefund = Math.Max(0m, remainingAllocated);
            }
            else
            {
                totalRefund = await ComputeLegacyCalendarRefundAsync(
                    subscription, subscriptionBeneficiary, beneficiaryTransactions, amountPerPayment, cancellationToken);
            }

            subscriptionBeneficiary.BudgetAllowance.AvailableFund += totalRefund;

            var transactionLogProductGroups = BuildRefundProductGroupBreakdown(subscriptionTypes, amountPerPayment, totalRefund);

            db.TransactionLogs.Add(new TransactionLog()
            {
                Discriminator = TransactionLogDiscriminator
                    .RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog,
                CreatedAtUtc = today,
                TotalAmount = totalRefund,
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
                OrganizationId = beneficiary.OrganizationId,
                OrganizationName = beneficiary.Organization.Name,
                SubscriptionId = subscription?.Id,
                SubscriptionName = subscription?.Name,
                ProjectId = beneficiary.Organization.ProjectId,
                ProjectName = beneficiary.Organization.Project.Name,
                TransactionLogProductGroups = transactionLogProductGroups,
                TransactionInitiatorId = currentUserId,
                TransactionInitiatorFirstname = currentUser?.Profile.FirstName,
                TransactionInitiatorLastname = currentUser?.Profile.LastName,
                TransactionInitiatorEmail = currentUser?.Email
            });
            
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] RemoveBeneficiaryFromSubscription - Beneficiary {beneficiary.Firstname} {beneficiary.Lastname} remove from subscription {subscription.Name}");
        }

        private async Task<decimal> ComputeLegacyCalendarRefundAsync(
            Subscription subscription,
            SubscriptionBeneficiary subscriptionBeneficiary,
            List<Transaction> beneficiaryTransactions,
            decimal amountPerPayment,
            CancellationToken cancellationToken)
        {
            var paymentsRemaining = await subscriptionBeneficiary.GetPaymentRemainingAsync(db, clock, cancellationToken);

            if (subscription.IsSubscriptionPaymentBasedCardUsage)
            {
                var rawTransactionCount = beneficiaryTransactions
                    .OfType<SubscriptionAddingFundTransaction>()
                    .Count(x => x.SubscriptionType.SubscriptionId == subscription.Id);

                var numberOfPaymentTypes = subscription.GetNumberOfPaymentTypes(subscriptionBeneficiary.BeneficiaryTypeId);
                var paymentsMade = SubscriptionHelper.GetNumberOfPaymentsMade(rawTransactionCount, numberOfPaymentTypes);

                var maxNumberOfPayments = subscriptionBeneficiary.GetEffectiveMaxNumberOfPayments();
                paymentsRemaining = Math.Max(0, Math.Min(paymentsRemaining, maxNumberOfPayments - paymentsMade));
            }

            return paymentsRemaining * amountPerPayment;
        }

        private static List<TransactionLogProductGroup> BuildRefundProductGroupBreakdown(
            List<SubscriptionType> subscriptionTypes, decimal amountPerPayment, decimal totalRefund)
        {
            var transactionLogProductGroups = new List<TransactionLogProductGroup>();
            var productGroups = subscriptionTypes.GroupBy(x => x.ProductGroupId).ToList();
            var allocated = 0m;

            for (var i = 0; i < productGroups.Count; i++)
            {
                var currentProductGroup = productGroups[i].First().ProductGroup;
                var groupAmount = i == productGroups.Count - 1
                    ? totalRefund - allocated
                    : amountPerPayment > 0
                        ? Math.Round(totalRefund * productGroups[i].Sum(x => x.Amount) / amountPerPayment, 2)
                        : 0m;
                allocated += groupAmount;

                transactionLogProductGroups.Add(new TransactionLogProductGroup()
                {
                    Amount = groupAmount,
                    ProductGroupId = currentProductGroup.Id,
                    ProductGroupName = currentProductGroup.Name
                });
            }

            return transactionLogProductGroups;
        }

        public class SubscriptionNotFoundException : RequestValidationException { }
        public class BeneficiaryNotFoundException : RequestValidationException { }
        public class BeneficiaryNotInSubscriptionException : RequestValidationException { }

        [MutationInput]
        public class Input : HaveSubscriptionIdAndBeneficiaryId, IRequest { }
    }
}
