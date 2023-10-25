using GraphQL.Conventions;
using GraphQL.DataLoader;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class PaymentFundGraphType
    {
        protected readonly PaymentFund paymentFund;

        public Id Id => paymentFund.GetIdentifier();

        public decimal Amount => paymentFund.Amount;

        public PaymentFundGraphType(PaymentFund paymentFund)
        {
            this.paymentFund = paymentFund;
        }

        public IDataLoaderResult<ProductGroupGraphType> ProductGroup(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProductGroup(paymentFund.ProductGroupId);
        }

        public IDataLoaderResult<OffPlatformBeneficiaryGraphType> Benificiary(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadOffPlatformBeneficiary(paymentFund.BeneficiaryId);
        }
    }
}
