using GraphQL.Conventions;
using GraphQL.DataLoader;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class TransactionGraphType : ITransactionGraphType
    {
        private readonly Transaction transaction;

        Id ITransactionGraphType.Id => GetId();
        private Id GetId()
        {
            switch (transaction.Discriminator)
            {
                case "LoyaltyAddingFundTransaction":
                {
                    return Id.New<LoyaltyAddingFundTransaction>(transaction.Id);
                }
                case "AddingFundTransaction":
                {
                    return Id.New<AddingFundTransaction>(transaction.Id);
                }
                case "ExpireFundTransaction":
                {
                    return Id.New<ExpireFundTransaction>(transaction.Id);
                }
                case "ManuallyAddingFundTransaction":
                {
                    return Id.New<ManuallyAddingFundTransaction>(transaction.Id);
                }
                case "OffPlatformAddingFundTransaction":
                {
                    return Id.New<OffPlatformAddingFundTransaction>(transaction.Id);
                }
                case "PaymentTransaction":
                {
                    return Id.New<PaymentTransaction>(transaction.Id);
                }
                case "PaymentTransactionProductGroup":
                {
                    return Id.New<PaymentTransactionProductGroup>(transaction.Id);
                }
                case "ProductGroupTransaction":
                {
                    return Id.New<ProductGroupTransaction>(transaction.Id);
                }
                case "RefundTransaction":
                {
                    return Id.New<RefundTransaction>(transaction.Id);
                }
                case "RefundTransactionProductGroup":
                {
                    return Id.New<RefundTransactionProductGroup>(transaction.Id);
                }
                case "SubscriptionAddingFundTransaction":
                {
                    return Id.New<SubscriptionAddingFundTransaction>(transaction.Id);
                }
            }

            return transaction.GetIdentifier();
        }

        public decimal Amount => transaction.Amount;

        public TransactionGraphType(Transaction transaction)
        {
            this.transaction = transaction;
        }

        public IDataLoaderResult<CardGraphType> Card(IAppUserContext ctx)
        {
            if (transaction.CardId.HasValue) return ctx.DataLoader.LoadCardById(transaction.CardId.Value);
            return null;
        }

        public OffsetDateTime CreatedAt()
        {
            return transaction.CreatedAtUtc.FromUtcToOffsetDateTime();
        }
    }
}
