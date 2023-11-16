namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class OrganizationStatsGraphType
    {
        public OrganizationGraphType Organization { get; set; }
        public decimal TotalActiveSubscriptionsEnvelopes { get; set; }
        public decimal TotalAllocatedOnCards { get; set; }
        public decimal RemainingPerEnvelope { get; set; }
        public decimal BalanceOnCards { get; set; }
        public decimal CardSpendingAmounts { get; set; }
        public decimal ExpiredAmounts { get; set; }
    }
}
