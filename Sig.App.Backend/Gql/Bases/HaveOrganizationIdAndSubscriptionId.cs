using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Bases
{
    public class HaveOrganizationIdAndSubscriptionId
    {
        public Id OrganizationId { get; set; }
        public Id SubscriptionId { get; set; }
    }
}
