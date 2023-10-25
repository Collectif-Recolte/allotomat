using Sig.App.Backend.Services.Mailer;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class ConfirmEmailEmail : EmailModel
    {
        public override string Subject => "Activation requise / Activation required";

        public string Token { get; set; }
        public string FirstName { get; set; }

        public ConfirmEmailEmail(string to, string token, string firstName) : base(to)
        {
            Token = token;
            FirstName = firstName;
        }
    }
}
