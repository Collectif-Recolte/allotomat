using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Requests.Queries.Projects;
using Sig.App.Backend.Services.Mailer;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.BackgroundJobs
{
    public class ExpireSubscription
    {
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly ILogger<ExpireSubscription> logger;
        private readonly IMediator mediator;
        private readonly IMailer mailer;

        public ExpireSubscription(AppDbContext db, IClock clock, ILogger<ExpireSubscription> logger, IMediator mediator, IMailer mailer)
        {
            this.db = db;
            this.clock = clock;
            this.logger = logger;
            this.mediator = mediator;
            this.mailer = mailer;
        }

        public static void RegisterJob(IConfiguration config)
        {
            RecurringJob.AddOrUpdate<ExpireSubscription>("ExpireSubscription",
                x => x.Run(),
                Cron.Daily(),
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"])
                });
        }

        public async Task Run()
        {
            var today = clock.GetCurrentInstant().ToDateTimeUtc();

            var subscriptions = await db.Subscriptions.Where(x => x.EndDate < today).ToListAsync();

            foreach (var subscription in subscriptions)
            {
                if (subscription.ExpirationNotificationSentDate == null && subscription.GetLastExpirationDate(clock) <= today)
                {
                    var projectManagers = await mediator.Send(new GetProjectProjectManagers.Query
                    {
                        ProjectId = subscription.ProjectId
                    });

                    var transactions = db.TransactionLogs.Where(x => x.SubscriptionId == subscription.Id);

                    subscription.ExpirationNotificationSentDate = DateTime.Now;
                    await mailer.Send(new SubscriptionExpirationEmail(string.Join(";", projectManagers.Select(x => x.Email)))
                    {
                        SubscriptionName = subscription.Name,
                        ExpiredAmount = transactions.Where(x => x.Discriminator == TransactionLogDiscriminator.ExpireFundTransactionLog).Sum(x => x.TotalAmount),
                        TotalAmountLoadedOnCards = transactions.Where(x => x.Discriminator == TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog || x.Discriminator == TransactionLogDiscriminator.ManuallyAddingFundTransactionLog).Sum(x => x.TotalAmount),
                        AmountUsedForPurchases = transactions.Where(x => x.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog || x.Discriminator == TransactionLogDiscriminator.RefundPaymentTransactionLog).Sum(x => x.Discriminator == TransactionLogDiscriminator.RefundPaymentTransactionLog ? -x.TotalAmount : x.TotalAmount)
                    });
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
