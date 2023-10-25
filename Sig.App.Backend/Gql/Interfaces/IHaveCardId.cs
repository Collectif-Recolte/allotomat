using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Interfaces
{
    public interface IHaveCardId
    {
        Id CardId { get; }
    }
}
