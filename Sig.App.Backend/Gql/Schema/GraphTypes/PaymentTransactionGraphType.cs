using GraphQL.Conventions;
using GraphQL.DataLoader;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using System.Collections.Generic;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class PaymentTransactionGraphType : ITransactionGraphType
    {
        private readonly PaymentTransaction transaction;

        public Id Id => transaction.GetIdentifier();
        public decimal Amount => transaction.Amount;

        public PaymentTransactionGraphType(PaymentTransaction transaction)
        {
            this.transaction = transaction;
        }

        public IDataLoaderResult<CardGraphType> Card(IAppUserContext ctx)
        {
            if (transaction.CardId.HasValue) return ctx.DataLoader.LoadCardById(transaction.CardId.Value);
            return null;
        }

        public IDataLoaderResult<MarketGraphType> Market(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadMarket(transaction.MarketId);
        }

        public OffsetDateTime CreatedAt()
        {
            return transaction.CreatedAtUtc.FromUtcToOffsetDateTime();
        }

        public IDataLoaderResult<IEnumerable<PaymentTransactionProductGroupGraphType>> TransactionByProductGroups(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadPaymentTransactionProductGroupByTransactionId(transaction.Id);
        }
    }

    public class PaymentTransactionProductGroupGraphType
    {
        private readonly PaymentTransactionProductGroup transaction;

        public Id Id => transaction.GetIdentifier();
        public decimal Amount => transaction.Amount;

        public PaymentTransactionProductGroupGraphType(PaymentTransactionProductGroup transaction)
        {
            this.transaction = transaction;
        }

        public IDataLoaderResult<PaymentTransactionGraphType> Transaction(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadPaymentTransactionById(transaction.PaymentTransactionId);
        }

        public IDataLoaderResult<ProductGroupGraphType> ProductGroup(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProductGroup(transaction.ProductGroupId);
        }
    }
}
