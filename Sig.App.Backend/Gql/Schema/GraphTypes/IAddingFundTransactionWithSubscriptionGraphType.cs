using GraphQL.DataLoader;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public interface IAddingFundTransactionWithSubscriptionGraphType : IAddingFundTransactionGraphType
    {
        IDataLoaderResult<SubscriptionGraphType> Subscription(IAppUserContext ctx);
    }
}
