using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Bases
{
    public class HaveProjectIdAndMarketId
    {
        public Id ProjectId { get; set; }
        public Id MarketId { get; set; }
    }
}
