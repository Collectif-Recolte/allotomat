using Sig.App.Backend.Services.Mailer;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class UserInviteEmail : EmailModel
    {
        public string FirstName { get; set; }
        public string InviteToken { get; set; }

        public override string Subject => "Vous avez été invité comme utilisateur / You have been invited as a user";

        public UserInviteEmail(string to) : base(to)
        {
        }
    }
}
