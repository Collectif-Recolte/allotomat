using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sig.App.Backend.DbModel.Entities.Transactions
{
    public class AddingFundTransaction : ProductGroupTransaction, IExpiringFundTransaction, ITransactionWithAvailableFund
    {
        public DateTime ExpirationDate { get; set; }

        public IList<PaymentTransactionAddingFundTransaction> PaymentTransactionAddingFundTransactions { get; set; }
        public IList<PaymentTransaction> Transactions { get; set; }
        public IList<ManuallyAddingFundTransaction> ManuallNegativeFundTransactions { get; set; }

        public decimal AvailableFund { get; set; }
        public FundTransactionStatus Status { get; set; }

        public long? ExpireFundTransactionId { get; set; }
        public ExpireFundTransaction ExpireFundTransaction { get; set; }

        public bool IsAlreadyUsed()
        {
            return Transactions.Any();
        }
    }
}
