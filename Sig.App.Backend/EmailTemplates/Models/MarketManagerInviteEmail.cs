using Sig.App.Backend.Services.Mailer;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class MarketManagerInviteEmail : EmailModel
    {
        public string MarketName { get; set; }
        public string InviteToken { get; set; }

        public override string Subject => "Vous êtes invité sur la plateforme Tomat / You are invited to the Tomat platform";

        public MarketManagerInviteEmail(string to) : base(to)
        {
        }
    }
}
