using GraphQL.DataLoader;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class BeneficiarySubscriptionTypeGraphType
    {
        private readonly Beneficiary beneficiary;
        private readonly Subscription subscription;

        public BeneficiarySubscriptionTypeGraphType(Beneficiary beneficiary, Subscription subscription)
        {
            this.beneficiary = beneficiary;
            this.subscription = subscription;
        }

        public IDataLoaderResult<IBeneficiaryGraphType> Beneficiary(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadBeneficiary(beneficiary.Id);
        }

        public IDataLoaderResult<SubscriptionGraphType> Subscription(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadSubscriptionById(subscription.Id);
        }

        public IDataLoaderResult<SubscriptionTypeGraphType> Type(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadSubscriptionTypeByBeneficiaryAndSubscriptionId(beneficiary.Id, subscription.Id);
        }
    }
}
