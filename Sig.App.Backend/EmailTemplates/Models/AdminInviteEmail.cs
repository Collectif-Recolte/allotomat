using Sig.App.Backend.Services.Mailer;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class AdminInviteEmail : EmailModel
    {
        public string FirstName { get; set; }
        public string InviteToken { get; set; }

        public override string Subject => "Vous avez été invité comme administrateur / You have been invited as an administrator";

        public AdminInviteEmail(string to, string token, string firstName) : base(to)
        {
            InviteToken = token;
            FirstName = firstName;
        }
    }
}