namespace Sig.App.Backend.DbModel.Enums;

public enum TransactionLogDiscriminator
{
    SubscriptionAddingFundTransactionLog = 0,
    ManuallyAddingFundTransactionLog = 1,
    LoyaltyAddingFundTransactionLog = 2,
    OffPlatformAddingFundTransactionLog = 3,
    ExpireFundTransactionLog = 4,
    PaymentTransactionLog = 5,
    TransferFundTransactionLog = 6,
    RefundBudgetAllowanceFromNoCardWhenAddingFundTransactionLog = 7,
    RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog = 8,
    RefundBudgetAllowanceFromUnassignedCardTransactionLog = 9,
    RefundPaymentTransactionLog = 10,
    LoyaltyEditFundTransactionLog = 11,
    AllocateBudgetAllowanceFromSubscriptionAssignmentTransactionLog = 12
}