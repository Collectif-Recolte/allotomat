using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Interfaces
{
    public interface IHaveMarketId
    {
        Id MarketId { get; }
    }
}
