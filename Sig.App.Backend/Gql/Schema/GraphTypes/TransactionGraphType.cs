using GraphQL.Conventions;
using GraphQL.DataLoader;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class TransactionGraphType : ITransactionGraphType
    {
        private readonly Transaction transaction;

        public Id Id => transaction.GetIdentifier();
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
    }
}
