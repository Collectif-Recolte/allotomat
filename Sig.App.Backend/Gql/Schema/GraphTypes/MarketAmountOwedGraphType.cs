namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class MarketAmountOwedGraphType
    {
        public MarketGraphType Market { get; set; }
        public decimal Amount { get; set; }
    }
}
