namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class SubscriptionEndTransactionGraphType
    {
        public SubscriptionGraphType Subscription { get; set; }

        public decimal TotalPurchases { get; set; }
        public decimal CardsWithFunds { get; set; }
        public decimal CardsUsedForPurchases { get; set; }
        public decimal MerchantsWithPurchases { get; set; }
        public decimal TotalFundsLoaded { get; set; }
        public decimal TotalPurchaseValue { get; set; }
        public decimal TotalExpiredAmount { get; set; }
    }
}
