using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Bases
{
    public class HaveMarketGroupIdAndMarketId
    {
        public Id MarketGroupId { get; set; }
        public Id MarketId { get; set; }
    }
}
