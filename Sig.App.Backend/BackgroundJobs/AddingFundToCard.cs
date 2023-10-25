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
            var cronFirstDayOfMonth = Cron.Monthly();
            RecurringJob.AddOrUpdate<AddingFundToCard>("AddingFundToCard:FirstDayOfTheMonth",
                x => x.Run(new SubscriptionMonthlyPaymentMoment[2] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth, SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth }),
                cronFirstDayOfMonth,
                TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"]));

            var cronFifteenDayOfMonth = Cron.Monthly(15);
            RecurringJob.AddOrUpdate<AddingFundToCard>("AddingFundToCard:FifteenthDayOfTheMonth",
                x => x.Run(new SubscriptionMonthlyPaymentMoment[2] { SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth, SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth }),
                cronFifteenDayOfMonth,
                TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"]));

            var cronWeekly = Cron.Weekly();
            RecurringJob.AddOrUpdate<AddingFundToCard>("AddingFundToCard:FirstDayOfTheWeek",
                x => x.Run(new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheWeek }),
                cronWeekly,
                TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"]));
        }

        public async Task Run(SubscriptionMonthlyPaymentMoment[] monthlyPaymentMoment)
        {
            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            var activeSubscriptions = await db.Subscriptions
                .Include(x => x.Beneficiaries).ThenInclude(x => x.Beneficiary).ThenInclude(x => x.Card).ThenInclude(x => x.Transactions)
                .Include(x => x.Beneficiaries).ThenInclude(x => x.Beneficiary).ThenInclude(x => x.Card).ThenInclude(x => x.Funds)
                .Include(x => x.Beneficiaries).ThenInclude(x => x.Beneficiary).ThenInclude(x => x.Organization)
                .Include(x => x.Beneficiaries).ThenInclude(x => x.BeneficiaryType)
                .Include(x => x.BudgetAllowances)
                .AsSplitQuery().Include(x => x.Types).ThenInclude(x => x.ProductGroup)
                .Where(x => x.StartDate <= today && x.EndDate >= today && monthlyPaymentMoment.Contains(x.MonthlyPaymentMoment)).ToListAsync();

            foreach (var subscription in activeSubscriptions)
            {
                foreach (var subscriptionBeneficiary in subscription.Beneficiaries)
                {
                    var beneficiary = subscriptionBeneficiary.Beneficiary;
                    var beneficiaryType = subscriptionBeneficiary.BeneficiaryType;
                    var subscriptionTypes = subscription.Types.Where(x => x.BeneficiaryTypeId == beneficiaryType.Id);
                    if (subscriptionBeneficiary.Beneficiary.Card != null)
                    {
                        var card = beneficiary.Card;
                        foreach (var subscriptionType in subscriptionTypes)
                        {
                            var transactionUniqueId = TransactionHelper.CreateTransactionUniqueId();
                            var now = clock.GetCurrentInstant().InUtc().ToDateTimeUtc();
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
                                ExpirationDate = subscription.GetExpirationDate(clock, subscription.MonthlyPaymentMoment),
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
                                TransactionLogProductGroups = transactionLogProductGroups
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

                            logger.LogInformation($"Adding fund {subscriptionType.Amount} for product group {subscriptionType.ProductGroupId} to ({subscriptionBeneficiary.BeneficiaryId}) card");
                        }
                    }
                    else
                    {
                        var budgetAllowance =
                            subscription.BudgetAllowances.First(x =>
                                x.OrganizationId == beneficiary.OrganizationId);
                        
                        // We refund the budget allowance
                        var transactionLogProductGroups = new List<TransactionLogProductGroup>();
                        foreach (var subscriptionType in subscriptionTypes)
                        {
                            budgetAllowance.AvailableFund += subscriptionType.Amount;
                            
                            logger.LogInformation($"Refund {subscriptionType.Amount} to the envelope for product group {subscriptionType.ProductGroupId}, organization {beneficiary.OrganizationId} and subscription {subscriptionType.SubscriptionId} because this participant has no cards : ({subscriptionBeneficiary.BeneficiaryId})");
                            
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
                            CreatedAtUtc = clock.GetCurrentInstant().InUtc().ToDateTimeUtc(),
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
                            TransactionLogProductGroups = transactionLogProductGroups
                        });
                    }
                }
            }

            var activeBeneficiaries = await db.Beneficiaries
                .Include(x => x.Card).ThenInclude(x => x.Funds)
                .Include(x => (x as OffPlatformBeneficiary).PaymentFunds).ThenInclude(x => x.ProductGroup)
                .Include(x => x.Organization)
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
                        var now = clock.GetCurrentInstant().InUtc().ToDateTimeUtc();
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

            await db.SaveChangesAsync();
        }
    }
}
