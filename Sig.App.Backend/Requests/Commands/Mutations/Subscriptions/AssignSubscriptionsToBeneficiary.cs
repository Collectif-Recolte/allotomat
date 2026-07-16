using GraphQL.Conventions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
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
    public class AssignSubscriptionsToBeneficiary : IRequestHandler<AssignSubscriptionsToBeneficiary.Input, AssignSubscriptionsToBeneficiary.Payload>
    {
        private readonly ILogger<AssignSubscriptionsToBeneficiary> logger;
        private IClock clock;
        private readonly AppDbContext db;
        private readonly IHttpContextAccessor httpContextAccessor;

        public AssignSubscriptionsToBeneficiary(ILogger<AssignSubscriptionsToBeneficiary> logger, IClock clock, AppDbContext db, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.clock = clock;
            this.db = db;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AssignSubscriptionsToBeneficiary({request.OrganizationId}, {request.Subscriptions}, {request.BeneficiaryId})");
            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations.Include(x => x.BudgetAllowances).Include(x => x.Project).FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null)
            {
                logger.LogWarning("[Mutation] AssignSubscriptionsToBeneficiary - OrganizationNotFoundException");
                throw new OrganizationNotFoundException();
            }

            var beneficiaryId = request.BeneficiaryId.LongIdentifierForType<Beneficiary>();
            var beneficiary = await db.Beneficiaries.Include(x => x.Subscriptions).Include(x => x.BeneficiaryType).Include(x => x.Card).FirstOrDefaultAsync(x => x.Id == beneficiaryId, cancellationToken);

            if (beneficiary == null)
            {
                logger.LogWarning("[Mutation] AssignSubscriptionsToBeneficiary - BeneficiaryNotFoundException");
                throw new BeneficiaryNotFoundException();
            }

            var subscriptionsLongIdentifiers = request.Subscriptions.Select(x => x.LongIdentifierForType<Subscription>());
            if (beneficiary.Subscriptions.Select(x => x.SubscriptionId).Intersect(subscriptionsLongIdentifiers).Any())
            {
                logger.LogWarning("[Mutation] AssignSubscriptionsToBeneficiary - BeneficiaryAlreadyGotSubscriptionException");
                throw new BeneficiaryAlreadyGotSubscriptionException();
            }

            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            IQueryable<Subscription> query = db.Subscriptions
                .Include(x => x.Beneficiaries)
                .Include(x => x.Types).ThenInclude(x => x.ProductGroup)
                .Where(x => subscriptionsLongIdentifiers.Contains(x.Id));
            Subscription[] subscriptions = query.ToArray();

            if (subscriptions.Length != subscriptionsLongIdentifiers.Count())
            {
                logger.LogWarning("[Mutation] AssignSubscriptionsToBeneficiary - SubscriptionNotFoundException");
                throw new SubscriptionNotFoundException();
            }

            var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            var currentUser = currentUserId != null
                ? db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId)
                : null;

            foreach (var subscription in query) {
                if (subscription.GetLastDateToAssignBeneficiary() < today)
                {
                    logger.LogWarning("[Mutation] AssignSubscriptionsToBeneficiary - SubscriptionAlreadyExpiredException");
                    throw new SubscriptionAlreadyExpiredException();
                }

                var budgetAllowance = organization.BudgetAllowances.FirstOrDefault(x => x.SubscriptionId == subscription.Id);

                if (budgetAllowance == null)
                {
                    logger.LogWarning("[Mutation] AssignSubscriptionsToBeneficiary - MissingBudgetAllowanceException");
                    throw new MissingBudgetAllowanceException();
                }

                if (!subscription.Types.Select(x => x.BeneficiaryTypeId).Any(x => x == beneficiary.BeneficiaryTypeId))
                {
                    logger.LogWarning("[Mutation] AssignSubscriptionsToBeneficiary - BeneficiaryTypeNotInSubscriptionException");
                    throw new BeneficiaryTypeNotInSubscriptionException();
                }

                var paymentRemaining = await subscription.GetPaymentRemainingAsync(db, clock, cancellationToken);

                if (subscription.IsSubscriptionPaymentBasedCardUsage)
                {
                    paymentRemaining = Math.Min(subscription.MaxNumberOfPayments.Value, paymentRemaining);
                }

                if (subscription.EndDate < today)
                {
                    subscription.Beneficiaries.Add(new SubscriptionBeneficiary()
                    {
                        BeneficiaryId = beneficiary.Id,
                        SubscriptionId = subscription.Id,
                        BudgetAllowanceId = budgetAllowance.Id,
                        BeneficiaryType = beneficiary.BeneficiaryType
                    });

                    logger.LogInformation($"[Mutation] AssignSubscriptionsToBeneficiary - Beneficiary {beneficiary.Firstname} {beneficiary.Lastname} added to subscription {subscription.Name}");
                }
                else
                {
                    var subscriptionTypes = subscription.Types.Where(x => x.BeneficiaryTypeId == beneficiary.BeneficiaryTypeId).ToList();
                    var amount = subscriptionTypes.Sum(x => x.Amount) * paymentRemaining;

                    if (budgetAllowance.AvailableFund >= amount)
                    {
                        subscription.Beneficiaries.Add(new SubscriptionBeneficiary()
                        {
                            BeneficiaryId = beneficiary.Id,
                            SubscriptionId = subscription.Id,
                            BudgetAllowanceId = budgetAllowance.Id,
                            BeneficiaryType = beneficiary.BeneficiaryType
                        });

                        budgetAllowance.AvailableFund -= amount;

                        if (amount > 0)
                        {
                            AddAllocationTransactionLog(beneficiary, organization, subscription, subscriptionTypes, paymentRemaining, amount, today, currentUserId, currentUser);
                        }

                        logger.LogInformation($"[Mutation] AssignSubscriptionsToBeneficiary - Beneficiary {beneficiary.Firstname} {beneficiary.Lastname} added to subscription {subscription.Name}");
                    }
                    else
                    {
                        logger.LogWarning("[Mutation] AssignSubscriptionsToBeneficiary - NotEnoughBudgetAllowanceException");
                        throw new NotEnoughBudgetAllowanceException();
                    }
                }
            }

            await db.SaveChangesAsync(cancellationToken);

            return new Payload()
            {
                Beneficiary = new BeneficiaryGraphType(beneficiary, organization.Project?.BeneficiariesAreAnonymous ?? true)
            };
        }

        private void AddAllocationTransactionLog(
            Beneficiary beneficiary,
            Organization organization,
            Subscription subscription,
            List<SubscriptionType> subscriptionTypes,
            int paymentRemaining,
            decimal amount,
            DateTime today,
            string currentUserId,
            AppUser currentUser)
        {
            var transactionLogProductGroups = new List<TransactionLogProductGroup>();
            foreach (var productGroup in subscriptionTypes.GroupBy(x => x.ProductGroupId))
            {
                var firstType = productGroup.First();
                transactionLogProductGroups.Add(new TransactionLogProductGroup()
                {
                    Amount = paymentRemaining * productGroup.Sum(x => x.Amount),
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
        public class Input : HaveOrganizationId, IRequest<Payload>
        {
            public Id BeneficiaryId { get; set; }
            public IEnumerable<Id> Subscriptions { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public BeneficiaryGraphType Beneficiary { get; set; }
        }

        public class OrganizationNotFoundException : RequestValidationException { }
        public class BeneficiaryNotFoundException : RequestValidationException { }
        public class SubscriptionAlreadyExpiredException : RequestValidationException { }
        public class MissingBudgetAllowanceException : RequestValidationException { }
        public class SubscriptionNotFoundException : RequestValidationException { }
        public class BeneficiaryAlreadyGotSubscriptionException : RequestValidationException { }
        public class BeneficiaryTypeNotInSubscriptionException : RequestValidationException { }
        public class NotEnoughBudgetAllowanceException : RequestValidationException { }
    }
}
