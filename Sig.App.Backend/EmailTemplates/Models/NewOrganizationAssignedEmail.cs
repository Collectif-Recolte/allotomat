using Sig.App.Backend.Services.Mailer;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class NewOrganizationAssignedEmail : EmailModel
    {
        public string OrganizationName { get; }
        public string OrganizationId { get; set; }

        public override string Subject => $"Bienvenue chez {OrganizationName} / Welcome to {OrganizationName}";

        public NewOrganizationAssignedEmail(string to, string organizationId, string organizationName) : base(to)
        {
            OrganizationId = organizationId;
            OrganizationName = organizationName;
        }
    }
}
