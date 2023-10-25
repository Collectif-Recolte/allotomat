using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Interfaces
{
    public interface IHaveOrganizationId
    {
        Id OrganizationId { get; }
    }
}
