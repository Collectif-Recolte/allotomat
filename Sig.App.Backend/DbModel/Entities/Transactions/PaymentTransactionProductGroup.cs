using Sig.App.Backend.DbModel.Entities.ProductGroups;
using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.Transactions
{
    public class PaymentTransactionProductGroup : IHaveLongIdentifier, IHaveProductGroup
    {
        public long Id { get; set; }
        public long PaymentTransactionId { get; set; }
        public PaymentTransaction PaymentTransaction { get; set; }

        public List<RefundTransactionProductGroup> RefundTransactionsProductGroup { get; set; }

        public decimal Amount { get; set; }
        
        public decimal RefundAmount { get; set; }

        public long ProductGroupId { get;set; }
        public ProductGroup ProductGroup { get; set; }
    }
}
