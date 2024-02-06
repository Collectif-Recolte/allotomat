using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Bases
{
    public class HaveBeneficiaryIdAndCardId
    {
        public Id BeneficiaryId { get; set; }
        public Id CardId { get; set; }
    }
}
