using GraphQL.Conventions;
using GraphQL.DataLoader;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class PaymentTransactionAddingFundTransactionGraphType
    {
        private readonly PaymentTransactionAddingFundTransaction paymentTransactionAddingFundTransaction;

        public PaymentTransactionAddingFundTransactionGraphType(PaymentTransactionAddingFundTransaction paymentTransactionAddingFundTransaction)
        {
            this.paymentTransactionAddingFundTransaction = paymentTransactionAddingFundTransaction;
        }

        public Id Id => paymentTransactionAddingFundTransaction.GetIdentifier();

        public decimal Amount => paymentTransactionAddingFundTransaction.Amount;
        public decimal RefundAmount => paymentTransactionAddingFundTransaction.RefundAmount;

        public IDataLoaderResult<PaymentTransactionGraphType> PaymentTransaction(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadPaymentTransactionById(paymentTransactionAddingFundTransaction.PaymentTransactionId);
        }

        public IDataLoaderResult<IAddingFundTransactionGraphType> AddingFundTransaction(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadAddingFundTransactionById(paymentTransactionAddingFundTransaction.AddingFundTransactionId);
        }
    }
}
