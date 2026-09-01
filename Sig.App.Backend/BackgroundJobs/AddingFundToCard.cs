using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using GraphQL.Conventions;
using Sig.App.Backend.Extensions;

namespace Sig.App.Backend.BackgroundJobs
{
    public class AddingFundToCard
    {
        private const int CardStatsBatchSize = 1000;

        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly ILogger<AddingFundToCard> logger;

        public AddingFundToCard(AppDbContext db, IClock clock, ILogger<AddingFundToCard> logger)
        {
            this.db = db;
            this.clock = clock;
            this.logger = logger;
        }

        public static void RegisterJob(IConfiguration config)
        {
            var cronFirstDayOfMonth = Cron.Monthly(1, 4);
            RecurringJob.AddOrUpdate<AddingFundToCard>(SubscriptionHelper.AddingFundToCardFirstDayOfTheMonthJobName,
                x => x.Run(SubscriptionHelper.AddingFundToCardFirstDayOfTheMonthJobName, new SubscriptionMonthlyPaymentMoment[2] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth, SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth }),
                cronFirstDayOfMonth,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"])
                });

            var cronFifteenDayOfMonth = Cron.Monthly(15, 4);
            RecurringJob.AddOrUpdate<AddingFundToCard>(SubscriptionHelper.AddingFundToCardFifteenthDayOfTheMonthJobName,
                x => x.Run(SubscriptionHelper.AddingFundToCardFifteenthDayOfTheMonthJobName, new SubscriptionMonthlyPaymentMoment[2] { SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth, SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth }),
                cronFifteenDayOfMonth,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"])
                });

            var cronWeekly = Cron.Weekly(DayOfWeek.Monday, 4);
            RecurringJob.AddOrUpdate<AddingFundToCard>(SubscriptionHelper.AddingFundToCardFirstDayOfTheWeekJobName,
                x => x.Run(SubscriptionHelper.AddingFundToCardFirstDayOfTheWeekJobName, new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheWeek }),
                cronWeekly,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"])
                });
        }

        public async Task Run(string name, SubscriptionMonthlyPaymentMoment[] monthlyPaymentMoment)
        {
            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            var lastRun = await db.AddingFundToCardRuns
                .Where(x => x.Name == name)
                .OrderBy(x => x.Id)
                .LastOrDefaultAsync();

            if (lastRun != null)
            {
                if (lastRun.Date.Month == today.Month && lastRun.Date.Day == today.Day)
                {
                    //Can't add fund multiple time in the same day
                    return;
                }
            }

            if (monthlyPaymentMoment.First() == SubscriptionMonthlyPaymentMoment.FirstDayOfTheWeek)
            {
                if (today.DayOfWeek != DayOfWeek.Monday)
                {
                    //Can't add fund on the first day of the week when it's not Monday
                    return;
                }
            }

            if (monthlyPaymentMoment.First() == SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth)
            {
                if (today.Day != 15)
                {
                    //Can't add fund on the fifteenth day of the month when it's not the 15th
                    return;
                }
            }

            if (monthlyPaymentMoment.First() == SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth)
            {
                if (today.Day != 1)
                {
                    //Can't add fund on the fifteenth day of the month when it's not the 15th
                    return;
                }
            }

            // CRCL-2675 : la fenêtre du job se compare en DATE, jamais en timestamp. StartDate et
            // EndDate sont stockés à minuit UTC alors que le run tombe à 08:00 UTC : avec
            // `EndDate >= today`, un abonnement dont le dernier versement tombe le jour même de sa
            // date de fin était exclu de son propre run. La réservation faite à l'assignation, elle,
            // compte ce versement (calcul calendaire, cf. SubscriptionHelper.GetTotalPayment) - le
            // montant restait donc réservé dans l'enveloppe, jamais livré, jamais remboursé.
            var todayDate = today.Date;

            var activeSubscriptions = await db.Subscriptions
                .Include(x => x.Beneficiaries).ThenInclude(x => x.Beneficiary).ThenInclude(x => x.Card).ThenInclude(x => x.Funds)
                .Include(x => x.Beneficiaries).ThenInclude(x => x.Beneficiary).ThenInclude(x => x.Organization).ThenInclude(x => x.Project)
                .Include(x => x.Beneficiaries).ThenInclude(x => x.BeneficiaryType)
                .AsSplitQuery()
                .Include(x => x.BudgetAllowances)
                .Include(x => x.Types).ThenInclude(x => x.ProductGroup)
                .Where(x => x.StartDate <= todayDate && x.EndDate >= todayDate && monthlyPaymentMoment.Contains(x.MonthlyPaymentMoment)).ToListAsync();

            var cardUsageStats = await LoadCardUsageStats(activeSubscriptions);

            foreach (var subscription in activeSubscriptions)
            {
                foreach (var subscriptionBeneficiary in subscription.Beneficiaries)
                {
                    await CreateTransaction(subscriptionBeneficiary, preloadedCardUsageStats: cardUsageStats);
                }
            }

            var activeBeneficiaries = await db.Beneficiaries
                .Include(x => x.Card).ThenInclude(x => x.Funds)
                .Include(x => (x as OffPlatformBeneficiary).PaymentFunds).ThenInclude(x => x.ProductGroup)
                .Include(x => x.Organization).ThenInclude(x => x.Project)
                .OfType<OffPlatformBeneficiary>()
                .Where(x => x.IsActive)
                .Where(x => x.StartDate <= todayDate && x.EndDate >= todayDate && monthlyPaymentMoment.Contains(x.MonthlyPaymentMoment.Value))
                .ToListAsync();

            foreach (var beneficiary in activeBeneficiaries)
            {
                foreach (var fund in beneficiary.PaymentFunds)
                {
                    if (beneficiary.Card != null)
                    {
                        var transactionUniqueId = TransactionHelper.CreateTransactionUniqueId();
                        var now = clock.GetCurrentInstant().ToDateTimeUtc();
                        db.Transactions.Add(new OffPlatformAddingFundTransaction()
                        {
                            TransactionUniqueId = transactionUniqueId,
                            Card = beneficiary.Card,
                            Beneficiary = beneficiary,
                            OrganizationId = beneficiary.OrganizationId,
                            Amount = fund.Amount,
                            AvailableFund = fund.Amount,
                            CreatedAtUtc = now,
                            ExpirationDate = SubscriptionHelper.GetNextPaymentDateTime(clock, beneficiary.MonthlyPaymentMoment.Value),
                            ProductGroup = fund.ProductGroup
                        });
                        
                        var transactionLogProductGroups = new List<TransactionLogProductGroup>()
                        {
                            new()
                            {
                                Amount = fund.Amount,
                                ProductGroupId = fund.ProductGroupId,
                                ProductGroupName = fund.ProductGroup.Name
                            }
                        };
                        
                        db.TransactionLogs.Add(new TransactionLog()
                        {
                            Discriminator = TransactionLogDiscriminator.OffPlatformAddingFundTransactionLog,
                            TransactionUniqueId = transactionUniqueId,
                            CreatedAtUtc = now,
                            TotalAmount = fund.Amount,
                            CardProgramCardId = beneficiary.Card.ProgramCardId,
                            CardNumber = beneficiary.Card.CardNumber,
                            BeneficiaryId = beneficiary.Id,
                            BeneficiaryID1 = beneficiary.ID1,
                            BeneficiaryID2 = beneficiary.ID2,
                            BeneficiaryFirstname = beneficiary.Firstname,
                            BeneficiaryLastname = beneficiary.Lastname,
                            BeneficiaryEmail = beneficiary.Email,
                            BeneficiaryPhone = beneficiary.Phone,
                            BeneficiaryIsOffPlatform = true,
                            BeneficiaryTypeId = beneficiary.BeneficiaryTypeId,
                            OrganizationId = beneficiary.OrganizationId,
                            OrganizationName = beneficiary.Organization.Name,
                            ProjectId = beneficiary.Organization.ProjectId,
                            ProjectName = beneficiary.Organization.Project.Name,
                            TransactionLogProductGroups = transactionLogProductGroups
                        });

                        var cardFund = beneficiary.Card.Funds.FirstOrDefault(x => x.ProductGroupId == fund.ProductGroup.Id);

                        if (cardFund == null)
                        {
                            cardFund = new Fund()
                            {
                                Card = beneficiary.Card,
                                ProductGroup = fund.ProductGroup
                            };

                            db.Funds.Add(cardFund);
                        }

                        cardFund.Amount = fund.Amount;
                    }
                }
            }

            db.AddingFundToCardRuns.Add(new DbModel.Entities.BackgroundJobs.AddingFundToCardRun()
            {
                Date = today,
                Name = name,
                Moments = monthlyPaymentMoment
            });

            await db.SaveChangesAsync();
        }

        public async Task AddFundToSpecificBeneficiary(Id beneficiaryId, BeneficiaryType beneficiaryType, Id subscriptionId, InitiatedBy initiatedBy = null)
        {
            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            var beneficiaryIdLong = beneficiaryId.LongIdentifierForType<Beneficiary>();
            var subscriptionIdLong = subscriptionId.LongIdentifierForType<Subscription>();

            var subscriptionBeneficiary = await db.SubscriptionBeneficiaries
                .Include(x => x.Subscription).ThenInclude(x => x.BudgetAllowances)
                .AsSplitQuery().Include(x => x.Subscription.Types).ThenInclude(x => x.ProductGroup)
                .Include(x => x.Beneficiary).ThenInclude(x => x.Card).ThenInclude(x => x.Funds)
                .Include(x => x.Beneficiary).ThenInclude(x => x.Organization).ThenInclude(x => x.Project)
                .Where(x => x.SubscriptionId == subscriptionIdLong && x.BeneficiaryId == beneficiaryIdLong && x.BeneficiaryTypeId == beneficiaryType.Id)
                .FirstOrDefaultAsync();

            if (subscriptionBeneficiary == null) return;

            await AddFundToExistingSubscriptionBeneficiary(subscriptionBeneficiary, initiatedBy);
            await db.SaveChangesAsync();
        }

        public async Task AddFundToExistingSubscriptionBeneficiary(SubscriptionBeneficiary subscriptionBeneficiary, InitiatedBy initiatedBy = null)
        {
            await CreateTransaction(subscriptionBeneficiary, initiatedBy);
        }

        private async Task CreateTransaction(SubscriptionBeneficiary subscriptionBeneficiary, InitiatedBy initiatedBy = null, CardUsageStatsCollection preloadedCardUsageStats = null)
        {
            if (subscriptionBeneficiary.Subscription == null)
            {
                subscriptionBeneficiary.Subscription = await db.Subscriptions
                    .Include(x => x.BudgetAllowances)
                    .Include(x => x.Types).ThenInclude(x => x.ProductGroup)
                    .FirstAsync(x => x.Id == subscriptionBeneficiary.SubscriptionId);
            }

            if (subscriptionBeneficiary.Beneficiary == null)
            {
                subscriptionBeneficiary.Beneficiary = await db.Beneficiaries
                    .Include(x => x.Card).ThenInclude(x => x.Funds)
                    .Include(x => x.Organization).ThenInclude(x => x.Project)
                    .AsSplitQuery()
                    .FirstAsync(x => x.Id == subscriptionBeneficiary.BeneficiaryId);
            }

            if (subscriptionBeneficiary.BeneficiaryType == null && subscriptionBeneficiary.BeneficiaryTypeId.HasValue)
            {
                subscriptionBeneficiary.BeneficiaryType = await db.BeneficiaryTypes
                    .FirstAsync(x => x.Id == subscriptionBeneficiary.BeneficiaryTypeId.Value);
            }

            var subscription = subscriptionBeneficiary.Subscription;
            var beneficiary = subscriptionBeneficiary.Beneficiary;
            var beneficiaryType = subscriptionBeneficiary.BeneficiaryType;

            var subscriptionTypes = subscription.Types.Where(x => x.BeneficiaryTypeId == beneficiaryType.Id).ToList();
            var amountPerPayment = subscriptionTypes.Sum(x => x.Amount);

            if (beneficiary.Card != null)
            {
                var card = beneficiary.Card;
                if (subscription.IsSubscriptionPaymentBasedCardUsage && initiatedBy == null)
                {
                    var previousPaymentDateTime = SubscriptionHelper.GetPreviousPaymentDateTime(clock, subscription.MonthlyPaymentMoment);
                    var cardStats = preloadedCardUsageStats != null
                        ? preloadedCardUsageStats.For(card.Id)
                        : (await LoadCardUsageStats(new[] { card.Id }, subscriptionTypes.Select(x => x.Id).ToList(), previousPaymentDateTime)).For(card.Id);

                    var subscriptionAddedFundCount = cardStats.CountSubscriptionAddingFund(subscriptionTypes);
                    var maxNumberOfPayments = subscriptionBeneficiary.GetEffectiveMaxNumberOfPayments();

                    var numberOfPaymentTypes = subscriptionTypes.Count();
                    var paymentsMade = SubscriptionHelper.GetNumberOfPaymentsMade(subscriptionAddedFundCount, numberOfPaymentTypes);

                    // The beneficiary already received all the funds
                    if (paymentsMade >= maxNumberOfPayments) return;

                    var usedCardSinceLastPayment = cardStats.LastPaymentTransactionDate >= previousPaymentDateTime;
                    if (paymentsMade != 0 && !usedCardSinceLastPayment)
                    {
                        if (maxNumberOfPayments - paymentsMade >= subscriptionBeneficiary.GetPaymentRemaining(clock, todaysFundJobCompleted: true))
                        {
                            RefundBudgetAllowance(subscriptionBeneficiary, subscriptionTypes);
                        }
                        return;
                    }
                }

                foreach (var subscriptionType in subscriptionTypes)
                {
                    var transactionUniqueId = TransactionHelper.CreateTransactionUniqueId();

                    var now = clock.GetCurrentInstant().ToDateTimeUtc();
                    db.Transactions.Add(new SubscriptionAddingFundTransaction()
                    {
                        TransactionUniqueId = transactionUniqueId,
                        Card = card,
                        Beneficiary = beneficiary,
                        OrganizationId = beneficiary.OrganizationId,
                        SubscriptionType = subscriptionType,
                        Amount = subscriptionType.Amount,
                        AvailableFund = subscriptionType.Amount,
                        CreatedAtUtc = now,
                        ExpirationDate = subscription.GetExpirationDate(clock),
                        ProductGroup = subscriptionType.ProductGroup
                    });

                    var transactionLogProductGroups = new List<TransactionLogProductGroup>()
                    {
                        new()
                        {
                            Amount = subscriptionType.Amount,
                            ProductGroupId = subscriptionType.ProductGroupId,
                            ProductGroupName = subscriptionType.ProductGroup.Name
                        }
                    };

                    db.TransactionLogs.Add(new TransactionLog()
                    {
                        Discriminator = TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog,
                        TransactionUniqueId = transactionUniqueId,
                        CreatedAtUtc = now,
                        TotalAmount = subscriptionType.Amount,
                        CardProgramCardId = card.ProgramCardId,
                        CardNumber = card.CardNumber,
                        BeneficiaryId = beneficiary.Id,
                        BeneficiaryID1 = beneficiary.ID1,
                        BeneficiaryID2 = beneficiary.ID2,
                        BeneficiaryFirstname = beneficiary.Firstname,
                        BeneficiaryLastname = beneficiary.Lastname,
                        BeneficiaryEmail = beneficiary.Email,
                        BeneficiaryPhone = beneficiary.Phone,
                        BeneficiaryIsOffPlatform = beneficiary is OffPlatformBeneficiary,
                        BeneficiaryTypeId = beneficiary.BeneficiaryTypeId,
                        OrganizationId = beneficiary.OrganizationId,
                        OrganizationName = beneficiary.Organization.Name,
                        SubscriptionId = subscription.Id,
                        SubscriptionName = subscription.Name,
                        ProjectId = beneficiary.Organization.ProjectId,
                        ProjectName = beneficiary.Organization.Project.Name,
                        TransactionLogProductGroups = transactionLogProductGroups,
                        TransactionInitiatorId = initiatedBy != null? initiatedBy.TransactionInitiatorId : null,
                        TransactionInitiatorFirstname = initiatedBy != null ? initiatedBy.TransactionInitiatorFirstname : null,
                        TransactionInitiatorLastname = initiatedBy != null ? initiatedBy.TransactionInitiatorLastname : null,
                        TransactionInitiatorEmail = initiatedBy != null ? initiatedBy.TransactionInitiatorEmail : null,
                        InitiatedByProject = initiatedBy != null ? true : false
                    });

                    var fund = card.Funds.FirstOrDefault(x => x.ProductGroupId == subscriptionType.ProductGroupId);
                    if (fund == null)
                    {
                        fund = new Fund()
                        {
                            Card = card,
                            ProductGroup = subscriptionType.ProductGroup
                        };

                        db.Funds.Add(fund);
                    }

                    fund.Amount += subscriptionType.Amount;

                    logger.LogInformation($"Adding fund {subscriptionType.Amount} for product group {subscriptionType.ProductGroupId} to ({beneficiary.Id}) card");
                }

                ConsumeAllocation(subscriptionBeneficiary, amountPerPayment);
            }
            else
            {
                if (subscription.IsSubscriptionPaymentBasedCardUsage)
                {
                    var maxNumberOfPayments = subscriptionBeneficiary.GetEffectiveMaxNumberOfPayments();
                    if (maxNumberOfPayments >= subscriptionBeneficiary.GetPaymentRemaining(clock, todaysFundJobCompleted: true))
                    {
                        RefundBudgetAllowance(subscriptionBeneficiary, subscriptionTypes);
                    }
                }
                else
                {
                    RefundBudgetAllowance(subscriptionBeneficiary, subscriptionTypes);
                }
            }
        }

        private void ConsumeAllocation(SubscriptionBeneficiary subscriptionBeneficiary, decimal amount)
        {
            subscriptionBeneficiary.AdjustAllocation(-amount, logger);
        }

        private Task<CardUsageStatsCollection> LoadCardUsageStats(IReadOnlyCollection<Subscription> subscriptions)
        {
            // Only payment based subscriptions look at the card history, so there is nothing
            // to load for the others.
            var paymentBasedSubscriptions = subscriptions.Where(x => x.IsSubscriptionPaymentBasedCardUsage).ToList();

            var cardIds = paymentBasedSubscriptions
                .SelectMany(x => x.Beneficiaries)
                .Where(x => x.Beneficiary?.Card != null)
                .Select(x => x.Beneficiary.Card.Id)
                .Distinct()
                .ToList();

            var subscriptionTypeIds = paymentBasedSubscriptions
                .SelectMany(x => x.Types)
                .Select(x => x.Id)
                .Distinct()
                .ToList();

            // The per subscription cutoff is applied in memory afterwards, so the earliest one
            // of the batch is used to fetch every payment that could possibly be relevant.
            var paymentTransactionsSince = paymentBasedSubscriptions.Count > 0
                ? paymentBasedSubscriptions.Min(x => SubscriptionHelper.GetPreviousPaymentDateTime(clock, x.MonthlyPaymentMoment))
                : DateTime.MaxValue;

            return LoadCardUsageStats(cardIds, subscriptionTypeIds, paymentTransactionsSince);
        }

        /// <summary>
        /// Aggregates the parts of the card history the job needs, instead of materializing every
        /// transaction of every card. The history grows forever while these aggregates do not, so
        /// hydrating it made the job slower every month until it hit the command timeout.
        /// </summary>
        private async Task<CardUsageStatsCollection> LoadCardUsageStats(IReadOnlyCollection<long> cardIds, IReadOnlyCollection<long> subscriptionTypeIds, DateTime paymentTransactionsSince)
        {
            var stats = new CardUsageStatsCollection();
            if (cardIds.Count == 0) return stats;

            foreach (var cardIdBatch in cardIds.Chunk(CardStatsBatchSize))
            {
                if (subscriptionTypeIds.Count > 0)
                {
                    var subscriptionAddedFundCounts = await db.Transactions.OfType<SubscriptionAddingFundTransaction>()
                        .Where(x => x.CardId != null && cardIdBatch.Contains(x.CardId.Value))
                        .Where(x => subscriptionTypeIds.Contains(x.SubscriptionTypeId))
                        .GroupBy(x => new { x.CardId, x.SubscriptionTypeId })
                        .Select(x => new { x.Key.CardId, x.Key.SubscriptionTypeId, Count = x.Count() })
                        .ToListAsync();

                    foreach (var subscriptionAddedFundCount in subscriptionAddedFundCounts)
                    {
                        stats.For(subscriptionAddedFundCount.CardId.Value)
                            .SetSubscriptionAddingFundCount(subscriptionAddedFundCount.SubscriptionTypeId, subscriptionAddedFundCount.Count);
                    }
                }

                var lastPaymentTransactions = await db.Transactions.OfType<PaymentTransaction>()
                    .Where(x => x.CardId != null && cardIdBatch.Contains(x.CardId.Value))
                    .Where(x => x.CreatedAtUtc >= paymentTransactionsSince)
                    .GroupBy(x => x.CardId)
                    .Select(x => new { CardId = x.Key, LastCreatedAtUtc = x.Max(y => y.CreatedAtUtc) })
                    .ToListAsync();

                foreach (var lastPaymentTransaction in lastPaymentTransactions)
                {
                    stats.For(lastPaymentTransaction.CardId.Value).LastPaymentTransactionDate = lastPaymentTransaction.LastCreatedAtUtc;
                }
            }

            return stats;
        }

        private void RefundBudgetAllowance(SubscriptionBeneficiary subscriptionBeneficiary, IReadOnlyCollection<SubscriptionType> subscriptionTypes)
        {
            var subscription = subscriptionBeneficiary.Subscription;
            var beneficiary = subscriptionBeneficiary.Beneficiary;
            var budgetAllowance = subscription.BudgetAllowances.First(x => x.OrganizationId == beneficiary.OrganizationId);

            // We refund the budget allowance
            var transactionLogProductGroups = new List<TransactionLogProductGroup>();
            foreach (var subscriptionType in subscriptionTypes)
            {
                budgetAllowance.AvailableFund += subscriptionType.Amount;

                logger.LogInformation($"Refund {subscriptionType.Amount} to the envelope for product group {subscriptionType.ProductGroupId}, organization {beneficiary.OrganizationId} and subscription {subscriptionType.SubscriptionId} because this participant has no cards : ({beneficiary.Id})");

                transactionLogProductGroups.Add(new TransactionLogProductGroup()
                {
                    Amount = subscriptionType.Amount,
                    ProductGroupId = subscriptionType.ProductGroupId,
                    ProductGroupName = subscriptionType.ProductGroup.Name
                });
            }

            ConsumeAllocation(subscriptionBeneficiary, subscriptionTypes.Sum(x => x.Amount));

            db.TransactionLogs.Add(new TransactionLog()
            {
                Discriminator = TransactionLogDiscriminator.RefundBudgetAllowanceFromNoCardWhenAddingFundTransactionLog,
                CreatedAtUtc = clock.GetCurrentInstant().ToDateTimeUtc(),
                TotalAmount = subscriptionTypes.Sum(x => x.Amount),
                BeneficiaryId = beneficiary.Id,
                BeneficiaryID1 = beneficiary.ID1,
                BeneficiaryID2 = beneficiary.ID2,
                BeneficiaryFirstname = beneficiary.Firstname,
                BeneficiaryLastname = beneficiary.Lastname,
                BeneficiaryEmail = beneficiary.Email,
                BeneficiaryPhone = beneficiary.Phone,
                BeneficiaryIsOffPlatform = beneficiary is OffPlatformBeneficiary,
                BeneficiaryTypeId = beneficiary.BeneficiaryTypeId,
                OrganizationId = beneficiary.OrganizationId,
                OrganizationName = beneficiary.Organization.Name,
                SubscriptionId = subscription.Id,
                SubscriptionName = subscription.Name,
                ProjectId = beneficiary.Organization.ProjectId,
                ProjectName = beneficiary.Organization.Project.Name,
                TransactionLogProductGroups = transactionLogProductGroups
            });
        }

        private class CardUsageStatsCollection
        {
            private readonly Dictionary<long, CardUsageStats> statsByCardId = new();

            public CardUsageStats For(long cardId)
            {
                if (!statsByCardId.TryGetValue(cardId, out var stats))
                {
                    stats = new CardUsageStats();
                    statsByCardId.Add(cardId, stats);
                }

                return stats;
            }
        }

        private class CardUsageStats
        {
            private readonly Dictionary<long, int> subscriptionAddingFundCountBySubscriptionType = new();

            public DateTime? LastPaymentTransactionDate { get; set; }

            public void SetSubscriptionAddingFundCount(long subscriptionTypeId, int count) =>
                subscriptionAddingFundCountBySubscriptionType[subscriptionTypeId] = count;

            public int CountSubscriptionAddingFund(IEnumerable<SubscriptionType> subscriptionTypes) =>
                subscriptionTypes.Sum(x => subscriptionAddingFundCountBySubscriptionType.GetValueOrDefault(x.Id));
        }

        public class InitiatedBy()
        {
            public string TransactionInitiatorId { get; set; }
            public string TransactionInitiatorFirstname { get; set; }
            public string TransactionInitiatorLastname { get; set; }
            public string TransactionInitiatorEmail { get; set; }
        }
    }
}
