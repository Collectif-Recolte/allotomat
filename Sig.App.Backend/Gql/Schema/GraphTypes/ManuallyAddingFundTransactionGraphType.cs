using GraphQL.Conventions;
using GraphQL.DataLoader;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class ManuallyAddingFundTransactionGraphType : IAddingFundTransactionWithSubscriptionGraphType
    {
        private readonly ManuallyAddingFundTransaction transaction;

        public Id Id => transaction.GetIdentifier();
        public decimal Amount => transaction.Amount;

        public decimal AvailableFund { get => transaction.AvailableFund; }
        public FundTransactionStatus Status { get => transaction.Status; }

        public ManuallyAddingFundTransactionGraphType(ManuallyAddingFundTransaction transaction)
        {
            this.transaction = transaction;
        }

        public OffsetDateTime ExpirationDate()
        {
            return transaction.ExpirationDate.FromUtcToOffsetDateTime();
        }

        public IDataLoaderResult<CardGraphType> Card(IAppUserContext ctx)
        {
            if (transaction.CardId.HasValue) return ctx.DataLoader.LoadCardById(transaction.CardId.Value);
            return null;
        }

        public IDataLoaderResult<ProductGroupGraphType> ProductGroup(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProductGroup(transaction.ProductGroupId);
        }

        public OffsetDateTime CreatedAt()
        {
            return transaction.CreatedAtUtc.FromUtcToOffsetDateTime();
        }

        public IDataLoaderResult<SubscriptionGraphType> Subscription(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadSubscriptionById(transaction.SubscriptionId);
        }
    }
}
