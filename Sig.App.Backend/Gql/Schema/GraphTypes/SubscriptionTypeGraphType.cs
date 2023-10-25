using GraphQL.Conventions;
using GraphQL.DataLoader;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class SubscriptionTypeGraphType
    {
        private readonly SubscriptionType subscriptionType;

        public Id Id => subscriptionType.GetIdentifier();
        public decimal Amount => subscriptionType.Amount;

        public SubscriptionTypeGraphType(SubscriptionType subscriptionType)
        {
            this.subscriptionType = subscriptionType;
        }

        public IDataLoaderResult<SubscriptionGraphType> Subscription(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadSubscriptionById(subscriptionType.SubscriptionId);
        }

        public IDataLoaderResult<BeneficiaryTypeGraphType> BeneficiaryType(IAppUserContext ctx)
        {
            if (subscriptionType.BeneficiaryTypeId.HasValue)
            {
                return ctx.DataLoader.LoadBeneficiaryType(subscriptionType.BeneficiaryTypeId.Value);
            }

            return null;
        }

        public IDataLoaderResult<ProductGroupGraphType> ProductGroup(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProductGroup(subscriptionType.ProductGroupId);
        }
    }
}
