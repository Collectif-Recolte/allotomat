using Sig.App.Backend.Services.Mailer;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class CreatedCardPdfEmail : EmailModel
    {
        public override string Subject => "Fichier pour impression de carte";

        public CreatedCardPdfEmail(string to) : base(to)
        {
        }
    }
}
