using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Services.Mailer;
using System.Collections.Generic;

namespace Sig.App.Backend.EmailTemplates.Models
{
    public class MonthlyBalanceReportEmail : EmailModel
    {
        public List<MarketBalanceReport> Reports { get; set; }
        public Project Project { get; set; }

        public override string Subject => $"Rapport mensuel de transaction pour le programme {Project.Name} / Monthly Transaction Report for program {Project.Name}";

        public MonthlyBalanceReportEmail(string to, List<MarketBalanceReport> reports, Project project) : base(to)
        {
            Reports = reports;
            Project = project;
        }

        public class MarketBalanceReport
        {
            public Market Market { get; set; }
            public decimal Total { get; set; }
        }
    }
}
