using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Interfaces
{
    public interface IHaveCardIdOrCardNumber
    {
        Id? CardId { get; set; }
        string CardNumber { get; set; }
    }
}
