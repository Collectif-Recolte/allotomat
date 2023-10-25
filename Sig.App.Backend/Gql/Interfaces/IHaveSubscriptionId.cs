using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Interfaces
{
    public interface IHaveSubscriptionId
    {
        Id SubscriptionId { get; }
    }
}
