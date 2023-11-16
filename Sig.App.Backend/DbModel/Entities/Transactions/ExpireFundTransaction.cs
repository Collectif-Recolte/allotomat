using Sig.App.Backend.DbModel.Entities.Subscriptions;

namespace Sig.App.Backend.DbModel.Entities.Transactions
{
    public class ExpireFundTransaction : ProductGroupTransaction
    {
        public long ExpiredSubscriptionId { get; set; }
        public Subscription ExpiredSubscription { get; set; }

        public long AddingFundTransactionId { get; set; }
        public AddingFundTransaction AddingFundTransaction { get; set; }
    }
}
