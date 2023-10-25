using Sig.App.Backend.Services.Mailer;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class ProjectManagerInviteEmail : EmailModel
    {
        public string ProjectName { get; set; }
        public string InviteToken { get; set; }

        public override string Subject => "Vous êtes invité sur la plateforme Tomat / You are invited to the Tomat platform";

        public ProjectManagerInviteEmail(string to) : base(to)
        {
        }
    }
}
