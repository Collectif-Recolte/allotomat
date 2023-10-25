namespace Sig.App.Backend.DbModel.Entities.Transactions
{
    public interface ITransactionWithAvailableFund
    {
        decimal AvailableFund { get; set; }
    }
}
