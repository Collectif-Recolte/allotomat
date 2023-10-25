using Sig.App.Backend.Services.Mailer;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class NewProjectAssignedEmail : EmailModel
    {
        public string ProjectName { get; }
        public string ProjectId { get; set; }

        public override string Subject => $"Bienvenue chez {ProjectName} / Welcome to {ProjectName}";

        public NewProjectAssignedEmail(string to, string projectId, string projectName) : base(to)
        {
            ProjectId = projectId;
            ProjectName = projectName;
        }
    }
}
