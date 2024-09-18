using Sig.App.Backend.Services.Mailer;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class NewMarketGroupAssignedEmail : EmailModel
    {
        public string MarketGroupName { get; }
        public string MarketGroupId { get; set; }

        public override string Subject => $"Bienvenue chez {MarketGroupName} / Welcome to {MarketGroupName}";

        public NewMarketGroupAssignedEmail(string to, string marketGroupId, string marketGroupName) : base(to)
        {
            MarketGroupId = marketGroupId;
            MarketGroupName = marketGroupName;
        }
    }
}
