using Sig.App.Backend.DbModel.Entities.CashRegisters;
using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.Transactions
{
    public class RefundTransaction : Transaction
    {
        public long? CashRegisterId { get; set; }
        public CashRegister? CashRegister { get; set; }

        public long InitialTransactionId { get; set; }
        public PaymentTransaction InitialTransaction { get; set; }
        public List<RefundTransactionProductGroup> RefundByProductGroups { get; set; }
        public bool InitiatedByProject { get; set; } = false;
        public bool InitiatedByOrganization { get; set; } = false;
    }
}
