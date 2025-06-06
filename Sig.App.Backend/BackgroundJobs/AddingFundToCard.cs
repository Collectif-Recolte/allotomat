﻿using Hangfire;
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
            RecurringJob.AddOrUpdate<AddingFundToCard>("AddingFundToCard:FirstDayOfTheMonth",
                x => x.Run("AddingFundToCard:FirstDayOfTheMonth", new SubscriptionMonthlyPaymentMoment[2] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth, SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth }),
                cronFirstDayOfMonth,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"])
                });

            var cronFifteenDayOfMonth = Cron.Monthly(15, 4);
            RecurringJob.AddOrUpdate<AddingFundToCard>("AddingFundToCard:FifteenthDayOfTheMonth",
                x => x.Run("AddingFundToCard:FifteenthDayOfTheMonth", new SubscriptionMonthlyPaymentMoment[2] { SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth, SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth }),
                cronFifteenDayOfMonth,
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"])
                });

            var cronWeekly = Cron.Weekly(DayOfWeek.Monday, 4);
            RecurringJob.AddOrUpdate<AddingFundToCard>("AddingFundToCard:FirstDayOfTheWeek",
                x => x.Run("AddingFundToCard:FirstDayOfTheWeek", new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheWeek }),
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

            var activeSubscriptions = await db.Subscriptions
                .Include(x => x.Beneficiaries).ThenInclude(x => x.Beneficiary).ThenInclude(x => x.Card).ThenInclude(x => x.Transactions)
                .Include(x => x.Beneficiaries).ThenInclude(x => x.Beneficiary).ThenInclude(x => x.Card).ThenInclude(x => x.Funds)
                .Include(x => x.Beneficiaries).ThenInclude(x => x.Beneficiary).ThenInclude(x => x.Organization).ThenInclude(x => x.Project)
                .Include(x => x.Beneficiaries).ThenInclude(x => x.BeneficiaryType)
                .Include(x => x.BudgetAllowances)
                .AsSplitQuery().Include(x => x.Types).ThenInclude(x => x.ProductGroup)
                .Where(x => x.StartDate <= today && x.EndDate >= today && monthlyPaymentMoment.Contains(x.MonthlyPaymentMoment)).ToListAsync();

            foreach (var subscription in activeSubscriptions)
            {
                foreach (var subscriptionBeneficiary in subscription.Beneficiaries)
                {
                    var beneficiary = subscriptionBeneficiary.Beneficiary;
                    CreateTransaction(beneficiary, subscriptionBeneficiary.BeneficiaryType, subscription);
                }
            }

            var activeBeneficiaries = await db.Beneficiaries
                .Include(x => x.Card).ThenInclude(x => x.Funds)
                .Include(x => (x as OffPlatformBeneficiary).PaymentFunds).ThenInclude(x => x.ProductGroup)
                .Include(x => x.Organization).ThenInclude(x => x.Project)
                .OfType<OffPlatformBeneficiary>()
                .Where(x => x.IsActive)
                .Where(x => x.StartDate <= today && x.EndDate >= today && monthlyPaymentMoment.Contains(x.MonthlyPaymentMoment.Value))
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

            var subscription = await db.Subscriptions
                .Include(x => x.BudgetAllowances)
                .AsSplitQuery().Include(x => x.Types).ThenInclude(x => x.ProductGroup)
                .Where(x => x.Id == subscriptionIdLong).FirstAsync();

            var beneficiary = await db.Beneficiaries
                .Include(x => x.Card).ThenInclude(x => x.Transactions)
                .Include(x => x.Card).ThenInclude(x => x.Funds)
                .Include(x => x.Organization).ThenInclude(x => x.Project)
                .Where(x => x.Id== beneficiaryIdLong)
                .FirstAsync();

            CreateTransaction(beneficiary, beneficiaryType, subscription, initiatedBy);
            await db.SaveChangesAsync();
        }

        private void CreateTransaction(Beneficiary beneficiary, BeneficiaryType beneficiaryType, Subscription subscription, InitiatedBy initiatedBy = null)
        {
            var subscriptionTypes = subscription.Types.Where(x => x.BeneficiaryTypeId == beneficiaryType.Id);

            if (beneficiary.Card != null)
            {
                var card = beneficiary.Card;
                if (subscription.IsSubscriptionPaymentBasedCardUsage && initiatedBy == null)
                {
                    var subscriptionAddedFundCount = beneficiary.Card.Transactions.OfType<SubscriptionAddingFundTransaction>().Count(x => subscriptionTypes.Any(y => y.Id == x.SubscriptionTypeId));

                    // The beneficiary already received all the funds
                    if (subscription.MaxNumberOfPayments.Value == subscriptionAddedFundCount * subscriptionTypes.Count()) return;

                    var previousPaymentDateTime = SubscriptionHelper.GetPreviousPaymentDateTime(clock, subscription.MonthlyPaymentMoment);
                    if (subscriptionAddedFundCount != 0 && !beneficiary.Card.Transactions.Where(x => x is PaymentTransaction).Any(x => x.CreatedAtUtc >= previousPaymentDateTime))
                    {
                        if (subscription.MaxNumberOfPayments - subscriptionAddedFundCount >= subscription.GetPaymentRemaining(clock))
                        {
                            RefundBudgetAllowance(subscription, beneficiary, subscriptionTypes);
                        }
                        return;
                    }
                }

                foreach (var subscriptionType in subscriptionTypes)
                {
                    var transactionUniqueId = TransactionHelper.CreateTransactionUniqueId();

                    var now = clock.GetCurrentInstant().ToDateTimeUtc();
                    card.Transactions.Add(new SubscriptionAddingFundTransaction()
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
            }
            else
            {
                if (subscription.IsSubscriptionPaymentBasedCardUsage)
                {
                    if (subscription.MaxNumberOfPayments >= subscription.GetPaymentRemaining(clock))
                    {
                        RefundBudgetAllowance(subscription, beneficiary, subscriptionTypes);
                    }
                }
                else
                {
                    RefundBudgetAllowance(subscription, beneficiary, subscriptionTypes);
                }
            }
        }

        private void RefundBudgetAllowance(Subscription subscription, Beneficiary beneficiary, IEnumerable<SubscriptionType> subscriptionTypes)
        {
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

        public class InitiatedBy()
        {
            public string TransactionInitiatorId { get; set; }
            public string TransactionInitiatorFirstname { get; set; }
            public string TransactionInitiatorLastname { get; set; }
            public string TransactionInitiatorEmail { get; set; }
        }
    }
}
