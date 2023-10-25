using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Interfaces
{
    public interface IHaveProjectId
    {
        Id ProjectId { get; }
    }
}
