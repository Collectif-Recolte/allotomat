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
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Bases;

namespace Sig.App.Backend.Requests.Commands.Mutations.Subscriptions
{
    public class AssignBeneficiariesToSubscription : IRequestHandler<AssignBeneficiariesToSubscription.Input, AssignBeneficiariesToSubscription.Payload>
    {
        private readonly ILogger<AssignBeneficiariesToSubscription> logger;
        private IClock clock;
        private readonly AppDbContext db;

        public AssignBeneficiariesToSubscription(ILogger<AssignBeneficiariesToSubscription> logger, IClock clock, AppDbContext db)
        {
            this.logger = logger;
            this.clock = clock;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AssignBeneficiariesToSubscription({request.OrganizationId}, {request.SubscriptionId}, {request.Amount}, {request.WithoutSubscription}, {request.WithSubscriptions}, {request.WithCategories}, {request.SearchText}, {request.Sort})");
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

            IQueryable<Beneficiary> query = db.Beneficiaries.Include(x => x.BeneficiaryType).Where(x => x.OrganizationId == organizationId);

            if (request.WithCategories?.Length > 0)
            {
                var categories = request.WithCategories.Select(x => x.LongIdentifierForType<BeneficiaryType>());
                query = query.Where(x => categories.Contains(x.BeneficiaryTypeId.GetValueOrDefault()));
            }

            if (request.WithSubscriptions?.Length > 0 || request.WithoutSubscription)
            {
                var subscriptions = request.WithSubscriptions.Select(x => x.LongIdentifierForType<Subscription>());
                query = query.Where(x => (request.WithoutSubscription && x.Subscriptions.Count == 0) || (subscriptions != null && x.Subscriptions.Any(y => subscriptions.Contains(y.SubscriptionId))));
            }

            if (!string.IsNullOrEmpty(request.SearchText))
            {
                var searchText = request.SearchText.Split(' ').AsEnumerable();

                foreach (var text in searchText)
                {
                    query = query.Where(x => x.ID1.Contains(text) || x.ID2.Contains(text) || x.Email.Contains(text) || x.Firstname.Contains(text) || x.Lastname.Contains(text));
                }
            }

            // Exclude beneficiary that already got this subscription
            query = query.Where(x => !x.Subscriptions.Any(y => y.SubscriptionId == subscriptionId));
            
            // Exclude beneficiary with beneficiaryType that are not include in the subcription
            var availableCategories = subscription.Types.Select(x => x.BeneficiaryTypeId).ToList();
            query = query.Where(x => availableCategories.Contains(x.BeneficiaryTypeId.GetValueOrDefault()));

            Beneficiary[] beneficiaries;

            if (request.Sort == AttributionSort.Default)
            {
                beneficiaries = query.OrderBy(x => x.SortOrder).ToArray();
            }
            else
            {
                //Random sort by new GUID to generate a new random order each time
                beneficiaries = query.OrderBy(_ => Guid.NewGuid()).ToArray();
            }

            var paymentRemaining = subscription.GetPaymentRemaining(clock);
            decimal totalAmount = 0;
            var beneficiariesWhoGetSubscriptions = 0;

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
                        $"Beneficiary {beneficiary.Firstname} {beneficiary.Lastname} added to subscription {subscription.Name}");
                }
            }
            else
            {
                foreach (var beneficiary in beneficiaries)
                {
                    var amount =
                        subscription.Types.Where(x => x.BeneficiaryTypeId == beneficiary.BeneficiaryTypeId)
                            .Sum(x => x.Amount) * paymentRemaining;

                    if (budgetAllowance.AvailableFund >= amount && totalAmount + amount <= request.Amount)
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

                        logger.LogInformation(
                            $"Beneficiary {beneficiary.Firstname} {beneficiary.Lastname} added to subscription {subscription.Name}");
                    }
                    else
                    {
                        break;
                    }
                }
            }

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
            public decimal Amount { get; set; }

            //Filters
            public bool WithoutSubscription { get; set; } = true;
            public Id[] WithSubscriptions { get; set; }
            public Id[] WithCategories { get; set; }
            public string SearchText { get; set; }

            //Sort
            public AttributionSort Sort { get; set; } = AttributionSort.Default;
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

        public enum AttributionSort
        {
            Default,
            Random
        }
    }
}
