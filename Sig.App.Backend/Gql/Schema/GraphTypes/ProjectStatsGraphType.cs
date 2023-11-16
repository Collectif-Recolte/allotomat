namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class ProjectStatsGraphType
    {
        public long BeneficiaryCount { get; set; }
        public decimal UnspentLoyaltyFund { get; set; }
        public decimal TotalActiveSubscriptionsEnvelopes { get; set; }
    }
}
