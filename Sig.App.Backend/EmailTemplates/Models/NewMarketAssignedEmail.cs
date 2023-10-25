using Sig.App.Backend.Services.Mailer;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class NewMarketAssignedEmail : EmailModel
    {
        public string MarketName { get; }
        public string MarketId { get; set; }

        public override string Subject => $"Bienvenue chez {MarketName} / Welcome to {MarketName}";

        public NewMarketAssignedEmail(string to, string marketId, string marketName) : base(to)
        {
            MarketId = marketId;
            MarketName = marketName;
        }
    }
}
