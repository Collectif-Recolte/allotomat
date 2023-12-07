using GraphQL.Conventions;
using GraphQL.DataLoader;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using System.Collections.Generic;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class RefundTransactionGraphType : ITransactionGraphType
    {
        private readonly RefundTransaction transaction;

        public Id Id => transaction.GetIdentifier();
        public decimal Amount => transaction.Amount;

        public RefundTransactionGraphType(RefundTransaction transaction)
        {
            this.transaction = transaction;
        }

        public IDataLoaderResult<CardGraphType> Card(IAppUserContext ctx)
        {
            if (transaction.CardId.HasValue) return ctx.DataLoader.LoadCardById(transaction.CardId.Value);
            return null;
        }

        public IDataLoaderResult<PaymentTransactionGraphType> InitialTransaction(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadPaymentTransactionById(transaction.InitialTransactionId);
        }

        public OffsetDateTime CreatedAt()
        {
            return transaction.CreatedAtUtc.FromUtcToOffsetDateTime();
        }

        public IDataLoaderResult<IEnumerable<RefundTransactionProductGroupGraphType>> TransactionByProductGroups(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadRefundTransactionsProductGroupByTransactionId(transaction.Id);
        }
    }

    public class RefundTransactionProductGroupGraphType
    {
        private readonly RefundTransactionProductGroup transaction;

        public Id Id => transaction.GetIdentifier();
        public decimal Amount => transaction.Amount;

        public RefundTransactionProductGroupGraphType(RefundTransactionProductGroup transaction)
        {
            this.transaction = transaction;
        }

        public IDataLoaderResult<RefundTransactionGraphType> Transaction(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadRefundTransactionById(transaction.RefundTransactionId);
        }

        public IDataLoaderResult<ProductGroupGraphType> ProductGroup(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProductGroup(transaction.ProductGroupId);
        }
    }
}
