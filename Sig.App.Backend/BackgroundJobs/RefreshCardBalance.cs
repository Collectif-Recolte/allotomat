using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoreLinq;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.BackgroundJobs
{
    public class RefreshCardBalance
    {
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly ILogger<RefreshCardBalance> logger;

        public RefreshCardBalance(AppDbContext db, IClock clock, ILogger<RefreshCardBalance> logger)
        {
            this.db = db;
            this.clock = clock;
            this.logger = logger;
        }

        public static void RegisterJob(IConfiguration config)
        {
            var cronNever = Cron.Never();
            RecurringJob.AddOrUpdate<RefreshCardBalance>("RefreshCardBalance:Never",
                x => x.Run(),
                cronNever,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"])
                });
        }

        public async Task Run()
        {
            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            var cards = await db.Cards
                .Include(x => x.Funds)
                .Include(x => x.Transactions)
                .Where(x => x.Status == CardStatus.Assigned).ToListAsync();

            foreach (var card in cards)
            {
                var transactionGroupByProductGroup = card.Transactions.OfType<AddingFundTransaction>().Where(x => x.Status == FundTransactionStatus.Actived).GroupBy(x => x.ProductGroupId);

                foreach (var productGroup in transactionGroupByProductGroup)
                {
                    var transactions = productGroup.ToList();
                    var fund = card.Funds.First(x => x.ProductGroupId == productGroup.Key);
                    var expectedAmount = transactions.Sum(x => x.AvailableFund);
                    var amount = fund.Amount;

                    if (expectedAmount > 0 && amount != expectedAmount)
                    {
                        fund.Amount = expectedAmount;
                        logger.LogInformation($"RefreshCardBalance :: Card with errors in balance (card : {card.Id} - fund : {fund.Id}) : {amount} -> {expectedAmount}");
                    }
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
