using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Subscriptions;

namespace Sig.App.Backend.DbModel.Entities.Transactions
{
    public class SubscriptionAddingFundTransaction : AddingFundTransaction
    {
        public long SubscriptionTypeId { get; set; }
        public SubscriptionType SubscriptionType { get; set; }
    }
}
