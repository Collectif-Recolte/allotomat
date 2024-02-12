using Sig.App.Backend.Services.Mailer;
using System.Collections.Generic;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class TransactionRefundBeneficiaryReceiptEmail : EmailModel
    {
        public string MarketName { get; set; }
        public string ProjectName { get; set; }
        public string ProjectUrl { get; set; }
        public decimal Amount { get; set; }
        public decimal TotalFund { get; set; }
        public IEnumerable<RefundProductGroupAvailableFund> ProductGroupAvailableFunds { get; set; }

        public override string Subject => $"Reçu de rembourssement chez {MarketName} / Reimbursement receipt from {MarketName}";

        public TransactionRefundBeneficiaryReceiptEmail(string to, string marketName, string projectName, string projectUrl, decimal amount, decimal totalFund, IEnumerable<RefundProductGroupAvailableFund> productGroupAvailableFunds) : base(to)
        {
            MarketName = marketName;
            ProjectName = projectName;
            ProjectUrl = projectUrl;
            Amount = amount;
            TotalFund = totalFund;
            ProductGroupAvailableFunds = productGroupAvailableFunds;
        }
    }

    public class RefundProductGroupAvailableFund
    {
        public string Name { get; set; }
        public decimal Fund { get; set; }
    }
}
