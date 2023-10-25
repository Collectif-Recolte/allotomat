using Sig.App.Backend.Services.Mailer;
using System.Collections.Generic;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class TransactionBeneficiaryReceiptEmail : EmailModel
    {
        public string MarketName { get; set; }
        public string ProjectName { get; set; }
        public string ProjectUrl { get; set; }
        public decimal Amount { get; set; }
        public decimal TotalFund { get; set; }
        public IEnumerable<ProductGroupAvailableFund> ProductGroupAvailableFunds { get; set; }

        public override string Subject => $"Reçu de transaction chez {MarketName} / Transaction receipt at {MarketName}";

        public TransactionBeneficiaryReceiptEmail(string to, string marketName, string projectName, string projectUrl, decimal amount, decimal totalFund, IEnumerable<ProductGroupAvailableFund> productGroupAvailableFunds) : base(to)
        {
            MarketName = marketName;
            ProjectName = projectName;
            ProjectUrl = projectUrl;
            Amount = amount;
            TotalFund = totalFund;
            ProductGroupAvailableFunds = productGroupAvailableFunds;
        }
    }

    public class ProductGroupAvailableFund
    {
        public string Name { get; set; }
        public decimal Fund { get; set; }
    }
}
