using Hangfire;
using Sig.App.Backend.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.Helpers;

namespace Sig.App.Backend.BackgroundJobs
{
    public class ExpireFundsFromCard
    {
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly ILogger<ExpireFundsFromCard> logger;

        public ExpireFundsFromCard(AppDbContext db, IClock clock, ILogger<ExpireFundsFromCard> logger)
        {
            this.db = db;
            this.clock = clock;
            this.logger = logger;
        }

        public static void RegisterJob(IConfiguration config)
        {
            var cronFirstDayOfMonth = Cron.Monthly();
            RecurringJob.AddOrUpdate<ExpireFundsFromCard>("ExpireFundsFromCard",
                x => x.Run(),
                Cron.Daily(0),
                new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"])
                });
        }

        public async Task Run()
        {
            var today = clock.GetCurrentInstant().ToDateTimeUtc();

            var dbTransactions = await db.Transactions.OfType<AddingFundTransaction>()
                .Include(x => x.ProductGroup)
                .Include(x => x.Card).ThenInclude(x => x.Funds)
                .Include(x => x.Beneficiary).ThenInclude(x => x.Organization).ThenInclude(x => x.Project)
                .Where(x => x.Status == FundTransactionStatus.Actived && x.ExpirationDate <= today).ToListAsync();

            var transactions = dbTransactions.Where(x =>
                x is ManuallyAddingFundTransaction or SubscriptionAddingFundTransaction
                    or OffPlatformAddingFundTransaction).ToList();
            var maftSubscriptions = await db.Subscriptions
                .Include(x => x.BudgetAllowances)
                .Where(x => transactions.OfType<ManuallyAddingFundTransaction>().Select(y => y.SubscriptionId).Contains(x.Id))
                .ToListAsync();
            var saftSubscriptionTypes = await db.SubscriptionTypes
                .Include(x => x.Subscription).ThenInclude(x => x.BudgetAllowances)
                .Where(x => transactions.OfType<SubscriptionAddingFundTransaction>().Select(x => x.SubscriptionTypeId)
                    .Contains(x.Id)).ToListAsync();

            var skippedTransactionCount = 0;

            foreach (var transaction in transactions)
            {
                if (transaction.AvailableFund > 0)
                {
                    var transactionProductGroupId = (transaction as IHaveProductGroup).ProductGroupId;
                    var fund = transaction.Card.Funds.FirstOrDefault(x => x.ProductGroupId == transactionProductGroupId);
                    if (fund == null)
                    {
                        skippedTransactionCount++;
                        logger.LogWarning($"ExpireFundsFromCard :: skipping transaction {transaction.Id} - " +
                            $"card {transaction.Card.ProgramCardId} ({transaction.Card.CardNumber}) has no Funds line " +
                            $"for product group {transactionProductGroupId}");
                        continue;
                    }

                    Subscription subscription = null;
                    if (transaction is ManuallyAddingFundTransaction maft)
                        subscription = maftSubscriptions.FirstOrDefault(x => x.Id == maft.SubscriptionId);
                    if (transaction is SubscriptionAddingFundTransaction saft)
                        subscription = saftSubscriptionTypes.FirstOrDefault(x => x.Id == saft.SubscriptionTypeId)?.Subscription;

                    // Expiring a payment is three moves that only count together: take the amount off the card
                    // total, give it back to the organization's envelope, mark the payment expired. The envelope
                    // move only applies when a subscription was resolved above, and the previous code took the
                    // allowance with `First` - a missing one threw mid-loop and, since the job saves once at the
                    // very end, rolled back the whole night, for every program. Bailing out here comes before the
                    // first of the three moves: the payment is left strictly intact (`continue` skips the
                    // AvailableFund and Status writes below) and stays repairable, and the rest of the pass
                    // expires normally.
                    BudgetAllowance budgetAllowance = null;
                    if (subscription != null)
                    {
                        budgetAllowance = subscription.BudgetAllowances.FirstOrDefault(x => x.OrganizationId == transaction.Beneficiary.OrganizationId);
                        if (budgetAllowance == null)
                        {
                            skippedTransactionCount++;
                            logger.LogWarning($"ExpireFundsFromCard :: skipping transaction {transaction.Id} - " +
                                $"card {transaction.Card.ProgramCardId} ({transaction.Card.CardNumber}) has no budget allowance " +
                                $"for organization {transaction.Beneficiary.OrganizationId} on subscription {subscription.Id}, " +
                                $"product group {transactionProductGroupId}");
                            continue;
                        }
                    }

                    fund.Amount -= transaction.AvailableFund;

                    if (budgetAllowance != null)
                    {
                        budgetAllowance.AvailableFund += transaction.AvailableFund;
                    }

                    var transactionUniqueId = TransactionHelper.CreateTransactionUniqueId();

                    var transactionLogProductGroups = new List<TransactionLogProductGroup>()
                    {
                        new()
                        {
                            Amount = transaction.AvailableFund,
                            ProductGroupId = transaction.ProductGroupId,
                            ProductGroupName = transaction.ProductGroup.Name
                        }
                    };

                    db.TransactionLogs.Add(new TransactionLog()
                    {
                        Discriminator = TransactionLogDiscriminator.ExpireFundTransactionLog,
                        TransactionUniqueId = transactionUniqueId,
                        CreatedAtUtc = today,
                        TotalAmount = transaction.AvailableFund,
                        CardProgramCardId = transaction.Card.ProgramCardId,
                        CardNumber = transaction.Card.CardNumber,
                        BeneficiaryId = transaction.BeneficiaryId,
                        BeneficiaryID1 = transaction.Beneficiary.ID1,
                        BeneficiaryID2 = transaction.Beneficiary.ID2,
                        BeneficiaryFirstname = transaction.Beneficiary.Firstname,
                        BeneficiaryLastname = transaction.Beneficiary.Lastname,
                        BeneficiaryEmail = transaction.Beneficiary.Email,
                        BeneficiaryPhone = transaction.Beneficiary.Phone,
                        BeneficiaryIsOffPlatform = transaction.Beneficiary is OffPlatformBeneficiary,
                        BeneficiaryTypeId = transaction.Beneficiary.BeneficiaryTypeId,
                        OrganizationId = transaction.Beneficiary.OrganizationId,
                        OrganizationName = transaction.Beneficiary.Organization.Name,
                        SubscriptionId = subscription?.Id,
                        SubscriptionName = subscription?.Name,
                        ProjectId = transaction.Beneficiary.Organization.ProjectId,
                        ProjectName = transaction.Beneficiary.Organization.Project.Name,
                        TransactionLogProductGroups = transactionLogProductGroups
                    });

                    var expireFundTransaction = new ExpireFundTransaction()
                    {
                        TransactionUniqueId = transactionUniqueId,
                        Amount = transaction.AvailableFund,
                        Card = transaction.Card,
                        CreatedAtUtc = today,
                        ProductGroupId = transactionProductGroupId,
                        ExpiredSubscription = subscription,
                        OrganizationId = transaction.OrganizationId,
                    };
                    transaction.Card.Transactions.Add(expireFundTransaction);
                    transaction.ExpireFundTransaction = expireFundTransaction;
                }

                transaction.AvailableFund = 0;
                transaction.Status = FundTransactionStatus.Expired;
            }

            if (skippedTransactionCount > 0)
            {
                logger.LogWarning($"ExpireFundsFromCard :: {skippedTransactionCount} transaction(s) skipped this run, " +
                    "available fund and status left untouched");
            }

            await db.SaveChangesWithFundRetryAsync();
        }
    }
}
