using GraphQL.Conventions;
using GraphQL.DataLoader;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class BeneficiarySubscriptionTypeGraphType
    {
        private readonly Beneficiary beneficiary;
        private readonly Subscription subscription;
        private readonly BeneficiaryType beneficiaryType;
        private readonly SubscriptionBeneficiary subscriptionBeneficiary;

        public BeneficiarySubscriptionTypeGraphType(Beneficiary beneficiary, Subscription subscription, BeneficiaryType beneficiaryType, SubscriptionBeneficiary subscriptionBeneficiary)
        {
            this.beneficiary = beneficiary;
            this.subscription = subscription;
            this.beneficiaryType = beneficiaryType;
            this.subscriptionBeneficiary = subscriptionBeneficiary;
        }

        public IDataLoaderResult<IBeneficiaryGraphType> Beneficiary(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadBeneficiary(beneficiary.Id);
        }

        public IDataLoaderResult<SubscriptionGraphType> Subscription(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadSubscriptionById(subscription.Id);
        }
        public IDataLoaderResult<BeneficiaryTypeGraphType> BeneficiaryType(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadBeneficiaryType(beneficiaryType.Id);
        }

        public IDataLoaderResult<IEnumerable<SubscriptionTypeGraphType>> Types(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadSubscriptionTypeByBeneficiaryAndSubscriptionId(beneficiary.Id, subscription.Id);
        }

        public async Task<int> PaymentReceived(IAppUserContext ctx)
        {
            var transactions = await ctx.DataLoader.LoadSubscriptionTransactionsByBeneficiaryAndSubscriptionId(beneficiary.Id, subscription.Id).GetResultAsync();
            return transactions.Count();
        }

        public async Task<int> PaymentRemaining(IAppUserContext ctx, [Inject] IClock clock)
        {
            var transactions = await ctx.DataLoader.LoadSubscriptionTransactionsByBeneficiaryAndSubscriptionId(beneficiary.Id, subscription.Id).GetResultAsync();
            var subscriptionPaymentRemaining = subscriptionBeneficiary.GetPaymentRemaining(clock);
            var maxNumberOfPayments = subscriptionBeneficiary.GetEffectiveMaxNumberOfPayments();

            return Math.Min(maxNumberOfPayments - transactions.Count(), subscriptionPaymentRemaining);
        }

        public async Task<int> AvailablePaymentRemaining(IAppUserContext ctx, [Inject] IClock clock)
        {
            return subscription.GetCardPaymentRemaining(clock);
        }

        public int MaxNumberOfPayments()
        {
            return subscriptionBeneficiary.GetEffectiveMaxNumberOfPayments();
        }

        public async Task<bool> HasMissedPayment(IAppUserContext ctx, [Inject] IClock clock) {
            var transactions = await ctx.DataLoader.LoadSubscriptionTransactionsByBeneficiaryAndSubscriptionId(beneficiary.Id, subscription.Id).GetResultAsync();
            var subscriptionTotalPayment = subscriptionBeneficiary.GetTotalPayment();
            var subscriptionPaymentRemaining = subscriptionBeneficiary.GetPaymentRemaining(clock);

            var now = clock.GetCurrentInstant().ToDateTimeUtc();
            var transactionCount = transactions.Count();
            var effectiveMaxPayments = subscriptionBeneficiary.GetEffectiveMaxNumberOfPayments();
            var previousPaymentCount = subscription.GetPreviousPaymentCount(clock);

            if (subscriptionBeneficiary.MaxNumberOfPaymentsOverride.HasValue || subscription.MaxNumberOfPayments.HasValue)
            {
                return subscription.GetExpirationDate(clock) > now
                    && subscription.GetFirstPaymentDateTime() < now
                    && transactionCount < effectiveMaxPayments
                    && previousPaymentCount > transactionCount;
            }

            return subscription.GetExpirationDate(clock) > now
                && subscription.GetFirstPaymentDateTime() < now
                && previousPaymentCount > transactionCount;
        }
    }
}
