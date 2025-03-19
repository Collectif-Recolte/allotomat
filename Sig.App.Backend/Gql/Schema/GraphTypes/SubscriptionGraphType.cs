using GraphQL.Conventions;
using GraphQL.DataLoader;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class SubscriptionGraphType
    {
        private readonly Subscription subscription;

        public Id Id => subscription.GetIdentifier();
        public NonNull<string> Name => subscription.Name;
        public SubscriptionMonthlyPaymentMoment MonthlyPaymentMoment => subscription.MonthlyPaymentMoment;
        public bool IsFundsAccumulable => subscription.IsFundsAccumulable;
        public bool IsSubscriptionPaymentBasedCardUsage => subscription.IsSubscriptionPaymentBasedCardUsage;
        public int? MaxNumberOfPayments => subscription.MaxNumberOfPayments;
        public FundsExpirationTrigger TriggerFundExpiration => subscription.TriggerFundExpiration;
        public int? NumberDaysUntilFundsExpire => subscription.NumberDaysUntilFundsExpire;

        public int TotalPayment => subscription.GetTotalPayment();

        public bool HasMissedPayment([Inject] IClock clock)
        {
            if (subscription.MaxNumberOfPayments.HasValue)
            {
                return subscription.GetFirstPaymentDateTime(clock) < clock.GetCurrentInstant().ToDateTimeUtc();
            }
            return subscription.GetPaymentRemaining(clock) < subscription.GetTotalPayment();
        }

        public SubscriptionGraphType(Subscription subscription)
        {
            this.subscription = subscription;
        }

        public IDataLoaderResult<ProjectGraphType> Project(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProject(subscription.ProjectId);
        }

        public int PaymentRemaining([Inject] IClock clock)
        {
            return subscription.GetPaymentRemaining(clock);
        }

        public OffsetDateTime StartDate()
        {
            return subscription.StartDate.FromUtcToOffsetDateTime();
        }

        public OffsetDateTime EndDate()
        {
            return subscription.EndDate.FromUtcToOffsetDateTime();
        }

        public OffsetDateTime? FundsExpirationDate()
        {
            if (subscription.FundsExpirationDate.HasValue)
            {
                return subscription.FundsExpirationDate.Value.FromUtcToOffsetDateTime();
            }

            return null;
        }

        public IDataLoaderResult<IEnumerable<SubscriptionTypeGraphType>> Types(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadSubscriptionSubscriptionTypes(Id.LongIdentifierForType<Subscription>());
        }

        public IDataLoaderResult<IEnumerable<BudgetAllowanceGraphType>> BudgetAllowances(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadSubscriptionBudgetAllowance(Id.LongIdentifierForType<Subscription>());
        }

        public async Task<decimal> BudgetAllowancesTotal(IAppUserContext ctx)
        {
            var budgetAllowances = await ctx.DataLoader.LoadSubscriptionBudgetAllowance(Id.LongIdentifierForType<Subscription>()).GetResultAsync();
            return budgetAllowances.Sum(x => x.AvailableFund);
        }

        public IDataLoaderResult<bool> HaveAnyBeneficiaries(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadSubscriptionHaveAnyBeneficiaries(Id.LongIdentifierForType<Subscription>());
        }

        public OffsetDateTime GetLastDateToAssignBeneficiary()
        {
            return subscription.GetLastDateToAssignBeneficiary().FromUtcToOffsetDateTime();
        }
    }
}
