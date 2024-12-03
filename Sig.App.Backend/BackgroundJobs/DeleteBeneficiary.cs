using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using System;
using System.Threading.Tasks;
using Sig.App.Backend.Services.Mailer;
using System.Linq;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace Sig.App.Backend.BackgroundJobs
{
    public class DeleteBeneficiary
    {
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly ILogger<DeleteBeneficiary> logger;
        private readonly IMailer mailer;
        
        public DeleteBeneficiary(AppDbContext db, IClock clock, ILogger<DeleteBeneficiary> logger, IMailer mailer)
        {
            this.db = db;
            this.clock = clock;
            this.logger = logger;
            this.mailer = mailer;
        }

        public static void RegisterJob(IConfiguration config)
        {
            RecurringJob.AddOrUpdate<DeleteBeneficiary>("DeleteBeneficiary",
                x => x.Run(),
                Cron.Daily(),
                new RecurringJobOptions() { TimeZone = TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"]) });
        }

        public async Task Run()
        {
            var today = clock.GetCurrentInstant().ToDateTimeUtc();
            var fy = today.AddYears(-5);
            
            var beneficiaries = await db.Beneficiaries
                .Include(x => x.Subscriptions).ThenInclude(x => x.Subscription)
                .Include(x => x.Card).ThenInclude(x => x.Transactions)
                .Where(x => x.CreatedAtUtc < fy).ToListAsync();
            foreach (var beneficiary in beneficiaries)
            {
                if (ExpirationDate(beneficiary) < DateTime.UtcNow)
                {
                    var transactionLogsToAnonymized = await db.TransactionLogs.Where(x => x.BeneficiaryId == beneficiary.Id).ToListAsync();
                    foreach (var transactionLog in transactionLogsToAnonymized)
                    {
                        transactionLog.BeneficiaryId = null;
                        transactionLog.BeneficiaryID1 = "-Anonymized-";
                        transactionLog.BeneficiaryID2 = "-Anonymized-";
                        transactionLog.BeneficiaryFirstname = "-Anonymized-";
                        transactionLog.BeneficiaryLastname = "-Anonymized-";
                        transactionLog.BeneficiaryEmail = "-Anonymized-";
                        transactionLog.BeneficiaryPhone = "-Anonymized-";
                    }

                    if (beneficiary.Card != null)
                    {
                        beneficiary.Card.Status = CardStatus.Deactivated;
                    }

                    if (!string.IsNullOrEmpty(beneficiary.Email))
                    {
                        var email = new DeleteUserEmail(beneficiary.Email, $"{beneficiary.Firstname} {beneficiary.Lastname}", today);
                        await mailer.Send(email);
                    }
                    db.Beneficiaries.Remove(beneficiary);
                }

                await db.SaveChangesAsync();
            }
        }

        private DateTime ExpirationDate(Beneficiary beneficiary)
        {
            if (beneficiary.Subscriptions.Where(x => x.Subscription.GetPaymentRemaining(clock) > 0 && x.Subscription.GetExpirationDate(clock) > DateTime.UtcNow).Any())
            {
                return DateTime.MaxValue;
            }

            if (beneficiary.Card != null)
            {
                if (beneficiary.Card.Transactions.Count() > 0)
                {
                    return beneficiary.Card.Transactions.OrderBy(x => x.CreatedAtUtc).Last().CreatedAtUtc.AddYears(5);
                }
            }

            return beneficiary.CreatedAtUtc.AddYears(5);
        }
    }
}
