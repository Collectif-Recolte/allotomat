using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Interfaces
{
    public interface IHaveBeneficiaryId
    {
        Id BeneficiaryId { get; }
    }
}
