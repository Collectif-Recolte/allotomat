using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Interfaces
{
    public interface IHaveOriginalCardId
    {
        Id OriginalCardId { get; }
    }
}
