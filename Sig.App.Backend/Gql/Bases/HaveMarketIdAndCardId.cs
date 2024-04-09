using GraphQL.Conventions;

namespace Sig.App.Backend.Gql.Bases
{
    public class HaveMarketIdAndCardId
    {
        public Id MarketId { get; set; }
        public Id CardId { get; set; }
    }
}
