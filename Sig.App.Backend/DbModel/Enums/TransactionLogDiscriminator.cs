namespace Sig.App.Backend.DbModel.Enums;

public enum TransactionLogDiscriminator
{
    SubscriptionAddingFundTransactionLog,
    ManuallyAddingFundTransactionLog,
    LoyaltyAddingFundTransactionLog,
    OffPlatformAddingFundTransactionLog,
    ExpireFundTransactionLog,
    PaymentTransactionLog,
    TransferFundTransactionLog,
    RefundBudgetAllowanceFromNoCardWhenAddingFundTransactionLog,
    RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog,
    RefundBudgetAllowanceFromUnassignedCardTransactionLog
}