using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

            var dbTransactions = await db.Transactions.OfType<AddingFundTransaction>()
                .Include(x => x.ProductGroup)
                .Include(x => x.Card).ThenInclude(x => x.Funds)
                .Where(x => x.Status == FundTransactionStatus.Actived).ToListAsync();

            var transactionsGroupByCard = dbTransactions.GroupBy(x => x.CardId);
            var cardWithErrors = new List<CardWithError>();

            foreach (var group in transactionsGroupByCard)
            {
                var card = group.First().Card;

                if (card.Status != CardStatus.Assigned)
                {
                    continue;
                }

                var transactionGroupByProductGroup = group.GroupBy(x => x.ProductGroupId);

                foreach (var productGroup in transactionGroupByProductGroup)
                {
                    var transactions = productGroup.ToList();
                    var fund = card.Funds.First(x => x.ProductGroupId == productGroup.Key);
                    var shouldBe = transactions.Sum(x => x.AvailableFund);
                    var isValue = fund.Amount;

                    if (shouldBe > 0 && isValue != shouldBe)
                    {
                        fund.Amount = shouldBe;

                        logger.LogInformation($"RefreshCardBalance :: Card with errors in balance (card : {card.Id} - fund : {fund.Id}) : {isValue} -> {shouldBe}");
                    }
                }
            }

            await db.SaveChangesAsync();
        }

        private class CardWithError
        {
            public Card Card { get; set; }
            public List<AddingFundTransaction> Transactions { get; set; }
            public decimal ShouldBe { get; set; }
            public decimal IsValue { get; set; }
            public Fund Fund { get; set; }
        }
    }
}
