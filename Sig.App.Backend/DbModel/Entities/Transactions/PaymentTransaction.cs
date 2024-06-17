using Sig.App.Backend.DbModel.Entities.Markets;
using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.Transactions
{
    public class PaymentTransaction : Transaction
    {
        public long MarketId { get; set; }
        public Market Market { get; set; }

        public List<PaymentTransactionAddingFundTransaction> PaymentTransactionAddingFundTransactions { get; set; }
        public List<AddingFundTransaction> Transactions { get; set; }
        public List<RefundTransaction> RefundTransactions { get; set; }

        public List<PaymentTransactionProductGroup> TransactionByProductGroups { get; set; }

        public bool InitiatedByProject { get; set; } = false;
    }
}
