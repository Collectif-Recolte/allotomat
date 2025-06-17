using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Services.Mailer;
using System.Collections.Generic;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class MonthlyCardBalanceReportEmail : EmailModel
    {
        public List<CardBalanceReport> Reports { get; set; }
        public Project Project { get; set; }

        public override string Subject => $"Rapport mensuel des cartes pour le programme {Project.Name} / Monthly cards Report for program {Project.Name}";

        public MonthlyCardBalanceReportEmail(string to, List<CardBalanceReport> reports, Project project) : base(to)
        {
            Reports = reports;
            Project = project;
        }

        public class CardBalanceReport
        {
            public Card Card { get; set; }
            public decimal Total { get; set; }
        }
    }
}
