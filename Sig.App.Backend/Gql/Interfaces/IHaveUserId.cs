using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Interfaces
{
    public interface IHaveUserId
    {
        Id UserId { get; }
    }
}