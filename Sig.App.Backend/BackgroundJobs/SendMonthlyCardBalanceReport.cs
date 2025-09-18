using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Requests.Queries.Cards;
using Sig.App.Backend.Requests.Queries.Projects;
using Sig.App.Backend.Services.Mailer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.BackgroundJobs
{
    public class SendMonthlyCardBalanceReport
    {
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly ILogger<SendMonthlyCardBalanceReport> logger;
        private readonly IMailer mailer;
        private readonly IMediator mediator;

        public SendMonthlyCardBalanceReport(AppDbContext db, IClock clock, ILogger<SendMonthlyCardBalanceReport> logger, IMailer mailer, IMediator mediator)
        {
            this.db = db;
            this.clock = clock;
            this.logger = logger;
            this.mailer = mailer;
            this.mediator = mediator;
        }
        public static void RegisterJob(IConfiguration config)
        {
            RecurringJob.AddOrUpdate<SendMonthlyCardBalanceReport>("SendMonthlyCardBalanceReport",
                x => x.Run(),
                Cron.Monthly(),
                new RecurringJobOptions() { TimeZone = TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"]) });
        }

        public async Task Run()
        {
            var cards = await db.Cards.Include(x => x.Beneficiary)
                .Include(x => x.Funds).ThenInclude(x => x.ProductGroup)
                .ToListAsync();

            var cardGroupByProject = cards.GroupBy(x => x.ProjectId).ToList();
            var projectIds = cardGroupByProject.Select(g => g.Key).ToList();
            var projects = await db.Projects.Where(p => projectIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

            foreach (var groupByProject in cardGroupByProject)
            {
                var cardBalanceReports = new List<CardBalanceReport>();

                foreach (var card in groupByProject)
                {
                    cardBalanceReports.Add(new CardBalanceReport()
                    {
                        Card = card,
                        Total = card.TotalFund()
                    });
                }

                var project = projects[groupByProject.Key];
                var projectManagers = await mediator.Send(new GetProjectProjectManagers.Query
                {
                    ProjectId = project.Id
                });

                var emailOptIn = EmailOptInHelper.GetEmailOptInMonthlyCardBalanceReport(clock.GetCurrentInstant().ToDateTimeUtc());
                projectManagers = projectManagers.Where(x => x.IsEmailOptedIn(emailOptIn)).ToList();

                if (projectManagers.Any())
                {
                    var email = new MonthlyCardBalanceReportEmail(string.Join(";", projectManagers.Select(x => x.Email)), cardBalanceReports, project);

                    var generator = new ExcelGenerator();
                    generator.AddDataWorksheet("Rapport des cartes mensuel", cardBalanceReports)
                        .Column("Numéro", x => x.Card.CardNumber)
                        .Column("ID de carte de programme", x => x.Card.ProgramCardId)
                        .Column("Status", x => CardHelper.GetCardStatus(x.Card.Status))
                        .Column("Fonds carte cadeau", x => MoneyHelper.GetMoneyFormat(x.Card.LoyaltyFund(), MoneyHelper.EN))
                        .Column("Fonds d'abonnement", x => MoneyHelper.GetMoneyFormat(x.Card.TotalSubscriptionFund(), MoneyHelper.EN))
                        .Column("Total", x => MoneyHelper.GetMoneyFormat(x.Total, MoneyHelper.EN));

                    email.Attachments = new List<EmailAttachmentModel>() { new EmailAttachmentModel("MonthlyCardsBalance.xlsx", ContentTypes.Xlsx, generator.Render()) };

                    await mailer.Send(email);
                }
                else
                {
                    logger.LogInformation($"Can't send monthly card report for {project.Name}. Reason: No project manager.");
                }
            }
        }
    }
}
