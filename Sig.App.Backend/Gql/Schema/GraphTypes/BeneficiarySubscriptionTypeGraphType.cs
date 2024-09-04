using GraphQL.Conventions;
using GraphQL.DataLoader;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Helpers;
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

        public BeneficiarySubscriptionTypeGraphType(Beneficiary beneficiary, Subscription subscription, BeneficiaryType beneficiaryType)
        {
            this.beneficiary = beneficiary;
            this.subscription = subscription;
            this.beneficiaryType = beneficiaryType;
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

        public async Task<bool> HasMissedPayment(IAppUserContext ctx, [Inject] IClock clock) {
            var transactions = await ctx.DataLoader.LoadSubscriptionTransactionsByBeneficiaryAndSubscriptionId(beneficiary.Id, subscription.Id).GetResultAsync();
            var subscriptionTotalPayment = subscription.GetTotalPayment();
            var subscriptionPaymentRemaining = subscription.GetPaymentRemaining(clock);

            return transactions.Count() < subscriptionTotalPayment - subscriptionPaymentRemaining;
        }
    }
}
