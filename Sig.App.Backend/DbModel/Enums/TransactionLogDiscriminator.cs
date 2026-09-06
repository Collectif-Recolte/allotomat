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
    AllocateBudgetAllowanceFromSubscriptionAssignmentTransactionLog = 12,

    // Réservation rendue à l'enveloppe parce que l'abonnement s'est terminé sans que le versement soit
    // jamais livré (CRCL-2676). Distinct du remboursement pour participant sans carte : ici la carte
    // existe, c'est le versement qui n'a jamais eu lieu.
    ReleaseBudgetAllowanceFromEndedSubscriptionTransactionLog = 13
}