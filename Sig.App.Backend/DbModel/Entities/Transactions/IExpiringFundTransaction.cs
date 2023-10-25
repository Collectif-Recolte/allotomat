using Sig.App.Backend.DbModel.Enums;
using System;

namespace Sig.App.Backend.DbModel.Entities.Transactions
{
    public interface IExpiringFundTransaction : ITransaction
    {
        DateTime ExpirationDate { get; set; }
        FundTransactionStatus Status { get; set; }
    }
}
