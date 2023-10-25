using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Queries.Subscriptions
{
    public class ForecastAssignBeneficiariesToSubscription : IRequestHandler<ForecastAssignBeneficiariesToSubscription.Input, ForecastAssignBeneficiariesToSubscription.Payload>
    {
        private IClock clock;
        private readonly AppDbContext db;

        public ForecastAssignBeneficiariesToSubscription(IClock clock, AppDbContext db)
        {
            this.clock = clock;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations.Include(x => x.BudgetAllowances).FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null) throw new OrganizationNotFoundException();

            var subscriptionId = request.SubscriptionId.LongIdentifierForType<Subscription>();
            var subscription = await db.Subscriptions.Include(x => x.Types).Include(x => x.Beneficiaries).FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

            if (subscription == null) throw new SubscriptionNotFoundException();

            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();
            if (subscription.GetLastDateToAssignBeneficiary() < today) throw new SubscriptionAlreadyExpiredException();

            var budgetAllowance = organization.BudgetAllowances.FirstOrDefault(x => x.SubscriptionId == subscriptionId);

            if (budgetAllowance == null) throw new MissingBudgetAllowanceException();

            IQueryable<Beneficiary> query = db.Beneficiaries.Include(x => x.BeneficiaryType).Where(x => x.OrganizationId == organizationId);

            if (request.WithCategories?.Length > 0)
            {
                var categories = request.WithCategories.Select(x => x.LongIdentifierForType<BeneficiaryType>());
                query = query.Where(x => categories.Contains(x.BeneficiaryTypeId.GetValueOrDefault()));
            }

            if (request.WithSubscriptions?.Length > 0)
            {
                var subscriptions = request.WithSubscriptions.Select(x => x.LongIdentifierForType<Subscription>());
                if(subscriptions != null)
                {
                    query = query.Where(x => (request.WithoutSubscription && x.Subscriptions.Count == 0) || x.Subscriptions.Any(y => subscriptions.Contains(y.SubscriptionId)));
                }
            }
            else if (request.WithoutSubscription)
            {
                query = query.Where(x => x.Subscriptions.Count == 0);
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
            var availableCategories = subscription.Types.Select(y => y.BeneficiaryTypeId).ToList();
            query = query.Where(x => availableCategories.Contains(x.BeneficiaryTypeId.GetValueOrDefault()));

            var beneficiaries = query.OrderBy(x => x.SortOrder).ToArray();
            var paymentRemaining = subscription.GetPaymentRemaining(clock);
            decimal totalAmount = 0;
            decimal availableFund = budgetAllowance.AvailableFund;
            var beneficiariesWhoGetSubscriptions = 0;

            if (subscription.EndDate < today)
            {
                beneficiariesWhoGetSubscriptions = beneficiaries.Length;
            }
            else
            {
                foreach (var beneficiary in beneficiaries)
                {
                    var amount =
                        subscription.Types.Where(x => x.BeneficiaryTypeId == beneficiary.BeneficiaryTypeId)
                            .Sum(x => x.Amount) * paymentRemaining;

                    if (availableFund >= amount && totalAmount + amount <= request.Amount)
                    {
                        availableFund -= amount;
                        totalAmount += amount;
                        beneficiariesWhoGetSubscriptions++;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return new Payload()
            {
                BeneficiariesWhoGetSubscriptions = beneficiariesWhoGetSubscriptions,
                TotalBeneficiaries = beneficiaries.Length,
                AvailableBudgetAfter = availableFund,
                UsageOfBudget = totalAmount
            };
        }

        public class Input : IRequest<Payload>, IHaveSubscriptionId, IHaveOrganizationId
        {
            public Id OrganizationId { get; set; }
            public Id SubscriptionId { get; set; }
            public decimal Amount { get; set; }

            //Filters
            public bool WithoutSubscription { get; set; } = true;
            public Id[] WithSubscriptions { get; set; }
            public Id[] WithCategories { get; set; }
            public string SearchText { get; set; }
        }

        public class Payload
        {
            public int BeneficiariesWhoGetSubscriptions { get; set; }
            public int TotalBeneficiaries { get; set; }
            public decimal AvailableBudgetAfter { get; set; }
            public decimal UsageOfBudget { get; set; }
        }

        public class OrganizationNotFoundException : RequestValidationException { }
        public class SubscriptionNotFoundException : RequestValidationException { }
        public class SubscriptionAlreadyExpiredException : RequestValidationException { }
        public class MissingBudgetAllowanceException : RequestValidationException { }
    }
}
