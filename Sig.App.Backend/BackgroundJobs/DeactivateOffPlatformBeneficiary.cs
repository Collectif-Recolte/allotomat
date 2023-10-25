using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.BackgroundJobs
{
    public class DeactivateOffPlatformBeneficiary
    {
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly ILogger<DeactivateOffPlatformBeneficiary> logger;

        public DeactivateOffPlatformBeneficiary(AppDbContext db, IClock clock, ILogger<DeactivateOffPlatformBeneficiary> logger)
        {
            this.db = db;
            this.clock = clock;
            this.logger = logger;
        }

        public static void RegisterJob(IConfiguration config)
        {
            var cronFirstDayOfMonth = Cron.Monthly();
            RecurringJob.AddOrUpdate<DeactivateOffPlatformBeneficiary>("DeactivateOffPlatformBeneficiary",
                x => x.Run(),
                Cron.Daily(),
                TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"]));
        }

        public async Task Run()
        {
            var today = clock.GetCurrentInstant().InUtc().ToDateTimeUtc();

            var activeBeneficiaries = await db.Beneficiaries.Where(x => x is OffPlatformBeneficiary && (x as OffPlatformBeneficiary).EndDate <= today && (x as OffPlatformBeneficiary).IsActive).Select(x => x as OffPlatformBeneficiary).ToListAsync();

            foreach (var beneficiary in activeBeneficiaries)
            {
                beneficiary.IsActive = false;
                beneficiary.PaymentFunds = new List<PaymentFund>();

                logger.LogInformation($"Off-platform beneficiary deactivated ({beneficiary.Id})");
            }

            await db.SaveChangesAsync();
        }
    }
}
