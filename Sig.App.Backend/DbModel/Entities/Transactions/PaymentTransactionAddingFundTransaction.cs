namespace Sig.App.Backend.DbModel.Entities.Transactions
{
    public class PaymentTransactionAddingFundTransaction : IHaveLongIdentifier
    {
        public long Id { get; set; }

        public long PaymentTransactionId { get; set; }
        public PaymentTransaction PaymentTransaction { get; set; }

        public long AddingFundTransactionId { get; set; }
        public AddingFundTransaction AddingFundTransaction { get; set; }

        public decimal Amount { get; set; }
        public decimal RefundAmount { get; set; }
    }
}
