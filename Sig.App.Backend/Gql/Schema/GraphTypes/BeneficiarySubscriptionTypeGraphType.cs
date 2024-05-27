using GraphQL.DataLoader;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.Gql.Interfaces;
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

        public IDataLoaderResult<SubscriptionTypeGraphType> Type(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadSubscriptionTypeByBeneficiaryAndSubscriptionId(beneficiary.Id, subscription.Id);
        }

        public async Task<int> PaymentReceived(IAppUserContext ctx)
        {
            var transactions = await ctx.DataLoader.LoadSubscriptionTransactionsByBeneficiaryAndSubscriptionId(beneficiary.Id, subscription.Id).GetResultAsync();
            return transactions.Count();
        }
    }
}
