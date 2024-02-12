using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.Transactions
{
    public class RefundTransaction : Transaction
    {
        public long InitialTransactionId { get; set; }
        public PaymentTransaction InitialTransaction { get; set; }
        public List<RefundTransactionProductGroup> RefundByProductGroups { get; set; }
        public bool InitiatedByProject { get; set; } = false;
    }
}
