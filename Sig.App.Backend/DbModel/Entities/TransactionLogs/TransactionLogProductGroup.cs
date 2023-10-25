namespace Sig.App.Backend.DbModel.Entities.TransactionLogs;

public class TransactionLogProductGroup : IHaveLongIdentifier
{
    public long Id { get; set; }
    public long TransactionLogId { get; set; }
    public TransactionLog TransactionLog { get; set; }
    public long ProductGroupId { get; set; }
    public string ProductGroupName { get; set; }
    public decimal Amount { get; set; }
}