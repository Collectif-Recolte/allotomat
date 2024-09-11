using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Bases
{
    public class HaveOrganizationIdAndMarketId
    {
        public Id OrganizationId { get; set; }
        public Id MarketId { get; set; }
    }
}
