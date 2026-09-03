namespace Sig.App.Backend.DbModel.Enums
{
    public enum BudgetAllowanceLogDiscriminator
    {
        CreateBudgetAllowanceLog,
        EditBudgetAllowanceLog,
        MoveBudgetAllowanceLog,
        DeleteBudgetAllowanceLog,

        // CRCL-2678 : crédit correctif d'un remboursement journalisé mais perdu sous concurrence.
        // Écrit uniquement par CreditLostBudgetAllowanceRefunds.
        CreditLostRefundBudgetAllowanceLog
    }
}
