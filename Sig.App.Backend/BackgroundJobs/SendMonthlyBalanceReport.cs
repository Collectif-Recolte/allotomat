using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Requests.Queries.Projects;
using Sig.App.Backend.Services.Mailer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Sig.App.Backend.EmailTemplates.Models.MonthlyBalanceReportEmail;

namespace Sig.App.Backend.BackgroundJobs
{
    public class SendMonthlyBalanceReport
    {
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly ILogger<SendMonthlyBalanceReport> logger;
        private readonly IMailer mailer;
        private readonly IMediator mediator;

        public SendMonthlyBalanceReport(AppDbContext db, IClock clock, ILogger<SendMonthlyBalanceReport> logger, IMailer mailer, IMediator mediator)
        {
            this.db = db;
            this.clock = clock;
            this.logger = logger;
            this.mailer = mailer;
            this.mediator = mediator;
        }
        public static void RegisterJob(IConfiguration config)
        {
            RecurringJob.AddOrUpdate<SendMonthlyBalanceReport>(
                x => x.Run(),
                Cron.Monthly(),
                TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"]));
        }

        public async Task Run()
        {
            var lastMonth = clock
                .GetCurrentInstant()
                .ToDateTimeUtc()
                .AddMonths(-1);

            var transactions = await db.Transactions
                .Include(x => x.Organization)
                .Where(x => x.CreatedAtUtc.Month == lastMonth.Month && x.CreatedAtUtc.Year == lastMonth.Year).ToListAsync();

            var refundTransactions = await db.Transactions
                .OfType<RefundTransaction>()
                .Include(x => x.Organization)
                .Include(x => x.InitialTransaction)
                .Where(x => x.CreatedAtUtc.Month == lastMonth.Month && x.CreatedAtUtc.Year == lastMonth.Year).ToListAsync();

            var transactionGroupByProject = transactions.Where(x => x.Organization != null).GroupBy(x => x.Organization.ProjectId).ToList();
            var refundTransactionGroupByProject = refundTransactions.Where(x => x.Organization != null).GroupBy(x => x.Organization.ProjectId).ToList();

            foreach (var groupByProject in transactionGroupByProject)
            {
                var marketBalanceReports = new List<MarketBalanceReport>();

                var project = await db.Projects.Where(x => x.Id == groupByProject.Key).FirstAsync();
                var projectManagers = await mediator.Send(new GetProjectProjectManagers.Query
                {
                    ProjectId = project.Id
                });

                var refundGroupByProject = refundTransactionGroupByProject.FirstOrDefault(x => x.Key == groupByProject.Key);
                var paymentTransactionGroupByMarket = groupByProject.Where(x => x.GetType() == typeof(PaymentTransaction)).Select(x => x as PaymentTransaction).GroupBy(x => x.MarketId).ToList();

                if (paymentTransactionGroupByMarket.Any())
                {
                    foreach (var groupByMarket in paymentTransactionGroupByMarket)
                    {
                        var market = await db.Markets.Where(x => x.Id == groupByMarket.Key).FirstAsync();

                        marketBalanceReports.Add(new MarketBalanceReport()
                        {
                            Market = market,
                            Total = groupByMarket.Sum(x => x.Amount)
                        });
                    }
                }

                if (refundGroupByProject != null)
                {
                    var refundTransactionGroupByMarket = refundGroupByProject.GroupBy(x => x.InitialTransaction.MarketId).ToList();

                    foreach (var refundGroupByMarket in refundTransactionGroupByMarket)
                    {
                        var market = await db.Markets.Where(x => x.Id == refundGroupByMarket.Key).FirstAsync();

                        var report = marketBalanceReports.FirstOrDefault(x => x.Market.Id == market.Id);
                        if (report != null) {
                            report.Total -= refundGroupByMarket.Sum(x => x.Amount);
                        }
                        else
                        {
                            marketBalanceReports.Add(new MarketBalanceReport()
                            {
                                Market = market,
                                Total = -refundGroupByMarket.Sum(x => x.Amount)
                            });
                        }
                    }
                }

                if (projectManagers.Any())
                {
                    var email = new MonthlyBalanceReportEmail(string.Join(";", projectManagers.Select(x => x.Email)), marketBalanceReports, project);

                    var generator = new ExcelGenerator();
                    generator.AddDataWorksheet("Rapport de solde mensuel", marketBalanceReports)
                        .Column("Nom du commerce", x => $"{(x.Market.IsArchived ? "[Archivé] " : "")}{x.Market.Name}")
                        .Column("Total à rembourser", x => x.Total);

                    email.Attachments = new List<EmailAttachmentModel>() { new EmailAttachmentModel("MonthlyBalance.xlsx", ContentTypes.Xlsx, generator.Render()) };

                    await mailer.Send(email);
                }
                else
                {
                    logger.LogInformation($"Can't send monthly balance report for {project.Name}. Reason: No project manager.");
                }
            }
        }
    }
}
