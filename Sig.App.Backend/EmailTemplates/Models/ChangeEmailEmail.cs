using Sig.App.Backend.Services.Mailer;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class ChangeEmailEmail : EmailModel
    {
        public override string Subject => "Avis de changement de courriel / Notification of change of email";

        public string NewEmail { get; }
        public string Token { get; }
        public string FirstName { get; }

        public ChangeEmailEmail(string newEmail, string token, string firstName) : base(to: newEmail)
        {
            NewEmail = newEmail;
            Token = token;
            FirstName = firstName;
        }
    }
}
