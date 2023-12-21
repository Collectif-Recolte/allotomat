using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Interfaces
{
    public interface IHaveInitialTransactionId
    {
        Id InitialTransactionId { get; }
    }
}
