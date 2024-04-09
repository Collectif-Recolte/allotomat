using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Bases
{
    public class HaveSubscriptionIdAndBeneficiaryId
    {
        public Id BeneficiaryId { get; set; }
        public Id SubscriptionId { get; set; }
    }
}
