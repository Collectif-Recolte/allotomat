using Sig.App.Backend.DbModel.Entities.ProductGroups;

namespace Sig.App.Backend.DbModel.Entities.Transactions
{
    public class RefundTransactionProductGroup : IHaveLongIdentifier, IHaveProductGroup
    {
        public long Id { get; set; }

        public long RefundTransactionId { get; set; }
        public RefundTransaction RefundTransaction { get; set; }

        public long PaymentTransactionProductGroupId { get; set; }
        public PaymentTransactionProductGroup PaymentTransactionProductGroup { get; set; }

        public decimal Amount { get; set; }

        public long ProductGroupId { get;set; }
        public ProductGroup ProductGroup { get; set; }
    }
}
