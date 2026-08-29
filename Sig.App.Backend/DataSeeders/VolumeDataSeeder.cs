using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Helpers;

namespace Sig.App.Backend.DataSeeders;

/// <summary>
/// Generates a dataset shaped after the production volumetry (participants, cards, funds,
/// transactions, organizations, subscriptions, subscription beneficiaries, budget allowances,
/// transaction logs), so that heavy background jobs can be exercised locally against a realistic
/// row count and relation shape instead of the handful of rows DevDataSeeder produces. Business
/// realism (names, addresses, amounts) is not attempted; only the volumes, the transaction type
/// proportions and the shape of the card/transaction relations are.
///
/// Refuses to run outside a Development environment, and refuses to run against a database that
/// already carries VolumeSeed data, complete or partial (for instance from an interrupted run): a
/// seed this large has plenty of opportunity to be interrupted, and resuming it correctly would
/// take more machinery than the problem is worth, so it asks for a clean database instead. Also
/// inert unless the VolumeSeed:Scale configuration key (environment variable VolumeSeed__Scale) is
/// set to a positive number: 1 targets the full production volume, 0.1 a tenth of it for a quick
/// pass. Startup only wires this seeder in place of DevDataSeeder under the same conditions; the
/// checks here are a second, self-contained line of defense so the class is safe to resolve from
/// any context.
/// </summary>
public class VolumeDataSeeder : IDataSeeder
{
    // Baseline volumes measured in production, used at Scale = 1.
    private const int BaselineOrganizations = 318;
    private const int BaselineParticipants = 21511;
    private const int BaselineCards = 33610;
    private const int BaselineActiveCards = 21542; // cards carrying at least one transaction
    private const int BaselineFunds = 36473;
    private const int BaselineTransactions = 317250;
    private const int BaselineSubscriptions = 109;
    private const int BaselineBudgetAllowances = 567;

    private const int MaxProjects = 8;
    private const int ParticipantBatchSize = 500;

    // Marks a dataset this seeder already produced, so a second run can refuse cleanly instead of
    // duplicating everything.
    private const string SeedMarkerPrefix = "VolumeSeed - Programme ";

    private enum TransactionKind
    {
        Payment,
        SubscriptionAddingFund,
        ExpireFund,
        ManuallyAddingFund,
        LoyaltyAddingFund
    }

    // Nominal transaction type distribution measured in production. The weights are normalized to
    // their own sum (not to 100) because the rounded percentages do not add up to exactly 100%.
    private static readonly (TransactionKind Kind, decimal Weight)[] TransactionTypeWeights = {
        (TransactionKind.Payment, 46m),
        (TransactionKind.SubscriptionAddingFund, 46m),
        (TransactionKind.ExpireFund, 5m),
        (TransactionKind.ManuallyAddingFund, 2m),
        (TransactionKind.LoyaltyAddingFund, 0.3m)
    };

    // Number of active cards, by transaction-count bucket, measured in production; their sum is
    // BaselineActiveCards. Used to shape the per-card transaction count: production has no long
    // tail (299 transactions on its busiest card), and most volume sits in the 21-50 bucket, not
    // at the extremes. A generator that spreads transactions uniformly would reproduce neither.
    private static readonly (int Min, int Max, int CardShare)[] TransactionCountBuckets = {
        (1, 5, 7893),
        (6, 20, 8077),
        (21, 50, 5101),
        (51, 100, 409),
        (101, 250, 60),
        (251, 300, 2)
    };

    private readonly AppDbContext db;
    private readonly ILogger<VolumeDataSeeder> logger;
    private readonly IClock clock;
    private readonly IConfiguration configuration;
    private readonly IWebHostEnvironment environment;
    private readonly Random random = new(1);

    public VolumeDataSeeder(AppDbContext db, ILogger<VolumeDataSeeder> logger, IClock clock, IConfiguration configuration, IWebHostEnvironment environment)
    {
        this.db = db;
        this.logger = logger;
        this.clock = clock;
        this.configuration = configuration;
        this.environment = environment;
    }

    public async Task Seed()
    {
        if (!environment.IsDevelopment())
        {
            logger.LogInformation("[VolumeDataSeeder] Seed skipped: not running in a Development environment");
            return;
        }

        if (!TryParseScale(configuration["VolumeSeed:Scale"], out var scale))
        {
            logger.LogInformation("[VolumeDataSeeder] Seed skipped: VolumeSeed:Scale is not set");
            return;
        }

        if (await db.Projects.AnyAsync(x => x.Name.StartsWith(SeedMarkerPrefix)))
        {
            throw new NonEmptyDatabaseException(
                "[VolumeDataSeeder] Refusing to seed: this database already carries VolumeSeed data, complete or " +
                "partial (for instance from an interrupted run). Restore a clean database before running the " +
                "volume seeder again.");
        }

        logger.LogInformation($"[VolumeDataSeeder] Seed(scale: {scale})");

        var now = clock.GetCurrentInstant().ToDateTimeUtc();

        var organizationsTotal = Scaled(BaselineOrganizations, scale);
        var participantsTotal = Scaled(BaselineParticipants, scale);
        var cardsTotal = Scaled(BaselineCards, scale);
        var activeCardsTotal = Math.Min(Math.Min(participantsTotal, cardsTotal), Scaled(BaselineActiveCards, scale));
        var fundsTotal = Math.Clamp(Scaled(BaselineFunds, scale), activeCardsTotal, activeCardsTotal * 2);
        var transactionsTotal = Scaled(BaselineTransactions, scale);
        var projectCount = Math.Clamp(organizationsTotal, 1, MaxProjects);
        var subscriptionsTotal = Math.Max(projectCount, Scaled(BaselineSubscriptions, scale));
        var budgetAllowancesTotal = Scaled(BaselineBudgetAllowances, scale);

        var projects = await CreateProjectsAsync(projectCount, now);
        var organizations = await CreateOrganizationsAsync(projects, organizationsTotal);
        await CreateSubscriptionsAsync(projects, subscriptionsTotal, now);
        var budgetAllowanceByOrganization = await CreateBudgetAllowancesAsync(projects, organizations, budgetAllowancesTotal);
        db.ChangeTracker.Clear();

        var loyaltyFundFlags = BuildLoyaltyFundAssignment(activeCardsTotal, fundsTotal - activeCardsTotal);
        var transactionSlotCounts = BuildTransactionSlotCounts(activeCardsTotal, transactionsTotal);

        var actualTransactionCounts = new Dictionary<TransactionKind, int>();
        foreach (var kind in Enum.GetValues<TransactionKind>()) actualTransactionCounts[kind] = 0;

        var stats = new SeedStats { Organizations = organizations.Count, Subscriptions = subscriptionsTotal, BudgetAllowances = budgetAllowancesTotal };

        var activeCardIndex = 0;

        for (var batchStart = 0; batchStart < participantsTotal; batchStart += ParticipantBatchSize)
        {
            var batchEnd = Math.Min(batchStart + ParticipantBatchSize, participantsTotal);
            var expirePairs = new List<(ExpireFundTransaction Expire, AddingFundTransaction Funding)>();
            var pendingLogBeneficiaries = new List<(TransactionLog Log, Beneficiary Beneficiary)>();

            for (var i = batchStart; i < batchEnd; i++)
            {
                var orgIndex = i % organizations.Count;
                var organization = organizations[orgIndex];
                var project = projects[organization.ProjectIndex];

                var beneficiary = new Beneficiary {
                    OrganizationId = organization.Id,
                    BeneficiaryTypeId = project.BeneficiaryTypeId,
                    Firstname = "VolumeSeed",
                    Lastname = $"Participant {i + 1}",
                    Address = "1 rue Exemple",
                    PostalCode = "A0A 0A0",
                    ID1 = (i + 1).ToString(CultureInfo.InvariantCulture),
                    SortOrder = i,
                    CreatedAtUtc = now
                };
                db.Beneficiaries.Add(beneficiary);
                stats.Participants++;

                // Every project carries at least one subscription (see CreateSubscriptionsAsync),
                // so every participant gets a SubscriptionBeneficiary row: this, and the
                // BudgetAllowances below, are exactly the two collections the monthly adding-fund
                // job iterates. Without them it finds no participant to pay and does nothing.
                var subscriptionBeneficiary = new SubscriptionBeneficiary {
                    SubscriptionId = project.SubscriptionId,
                    BeneficiaryTypeId = project.BeneficiaryTypeId,
                    BudgetAllowanceId = budgetAllowanceByOrganization.TryGetValue(orgIndex, out var allowanceId) ? allowanceId : null
                };
                subscriptionBeneficiary.Beneficiary = beneficiary;
                db.SubscriptionBeneficiaries.Add(subscriptionBeneficiary);
                stats.SubscriptionBeneficiaries++;

                if (activeCardIndex < activeCardsTotal)
                {
                    var card = new Card {
                        ProjectId = project.Id,
                        Status = CardStatus.Assigned,
                        ProgramCardId = activeCardIndex + 1,
                        CardNumber = $"VOLU-{activeCardIndex + 1:D8}"
                    };
                    card.Beneficiary = beneficiary;
                    beneficiary.Card = card;
                    db.Cards.Add(card);
                    stats.Cards++;

                    var mainFund = new Fund { ProductGroupId = project.MainProductGroupId, Amount = 0 };
                    mainFund.Card = card;
                    db.Funds.Add(mainFund);
                    stats.Funds++;

                    Fund loyaltyFund = null;
                    if (loyaltyFundFlags[activeCardIndex])
                    {
                        loyaltyFund = new Fund { ProductGroupId = project.LoyaltyProductGroupId, Amount = 0 };
                        loyaltyFund.Card = card;
                        db.Funds.Add(loyaltyFund);
                        stats.Funds++;
                    }

                    GenerateCardTransactions(
                        card, beneficiary, organization, project, mainFund, loyaltyFund,
                        transactionSlotCounts[activeCardIndex], now,
                        actualTransactionCounts, expirePairs, pendingLogBeneficiaries, stats);

                    activeCardIndex++;
                }
            }

            await db.SaveChangesAsync();

            // TransactionLog has no EF relationship to Beneficiary (it is a flat, denormalized
            // snapshot table), so BeneficiaryId can only be filled in once the beneficiary has been
            // saved and has a real id, exactly like AddingFundTransactionId on an expiration below.
            if (expirePairs.Count > 0)
            {
                foreach (var (expire, funding) in expirePairs) expire.AddingFundTransactionId = funding.Id;
            }
            foreach (var (log, beneficiary) in pendingLogBeneficiaries) log.BeneficiaryId = beneficiary.Id;

            if (expirePairs.Count > 0 || pendingLogBeneficiaries.Count > 0)
            {
                await db.SaveChangesAsync();
            }

            db.ChangeTracker.Clear();
        }

        var unassignedCardsRemaining = cardsTotal - activeCardsTotal;
        for (var i = 0; i < unassignedCardsRemaining; i++)
        {
            var project = projects[i % projects.Count];
            db.Cards.Add(new Card {
                ProjectId = project.Id,
                Status = CardStatus.Unassigned,
                ProgramCardId = activeCardsTotal + i + 1,
                CardNumber = $"VOLU-{activeCardsTotal + i + 1:D8}"
            });
            stats.Cards++;

            if ((i + 1) % 5000 == 0)
            {
                await db.SaveChangesAsync();
                db.ChangeTracker.Clear();
            }
        }

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        LogSummary(scale, stats, actualTransactionCounts);
    }

    private static bool TryParseScale(string raw, out decimal scale)
    {
        scale = 0;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        return decimal.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out scale) && scale > 0;
    }

    private static int Scaled(int baseline, decimal scale) =>
        Math.Max(1, (int)Math.Round(baseline * scale, MidpointRounding.AwayFromZero));

    // Splits `total` across the given weights using the largest-remainder method, so the parts sum
    // to exactly `total` while staying proportional to the weights.
    private static int[] AllocateByWeight(int total, IReadOnlyList<decimal> weights)
    {
        var weightSum = weights.Sum();
        var exact = weights.Select(w => weightSum > 0 ? total * w / weightSum : 0).ToArray();
        var floors = exact.Select(e => (int)Math.Floor(e)).ToArray();
        var remainder = total - floors.Sum();
        var order = Enumerable.Range(0, exact.Length).OrderByDescending(idx => exact[idx] - floors[idx]).ToArray();
        for (var i = 0; i < remainder && i < order.Length; i++) floors[order[i]]++;
        return floors;
    }

    // Decides, for every active card, whether it also carries a loyalty Fund row (a second
    // product group per card, matching the ~69% of active cards that do in production).
    private bool[] BuildLoyaltyFundAssignment(int activeCardsTotal, int loyaltyFundsTarget)
    {
        var flags = new bool[activeCardsTotal];
        loyaltyFundsTarget = Math.Clamp(loyaltyFundsTarget, 0, activeCardsTotal);

        var indices = Enumerable.Range(0, activeCardsTotal).OrderBy(_ => random.Next()).Take(loyaltyFundsTarget);
        foreach (var index in indices) flags[index] = true;
        return flags;
    }

    // Draws a transaction-count bucket per active card (matching production's per-card shape),
    // then rescales the raw draws so their sum lands exactly on `transactionsTotal`.
    private int[] BuildTransactionSlotCounts(int activeCardsTotal, int transactionsTotal)
    {
        var bucketWeights = TransactionCountBuckets.Select(b => (decimal)b.CardShare).ToArray();
        var bucketCounts = AllocateByWeight(activeCardsTotal, bucketWeights);

        var rawWeights = new decimal[activeCardsTotal];
        var cardIndex = 0;
        for (var b = 0; b < TransactionCountBuckets.Length; b++)
        {
            var (min, max, _) = TransactionCountBuckets[b];
            for (var c = 0; c < bucketCounts[b] && cardIndex < activeCardsTotal; c++, cardIndex++)
            {
                rawWeights[cardIndex] = random.Next(min, max + 1);
            }
        }
        while (cardIndex < activeCardsTotal) rawWeights[cardIndex++] = 1; // rounding leftover, if any

        return AllocateByWeight(transactionsTotal, rawWeights);
    }

    private TransactionKind SampleKind()
    {
        var weightSum = TransactionTypeWeights.Sum(w => w.Weight);
        var draw = (decimal)random.NextDouble() * weightSum;
        var cumulative = 0m;
        foreach (var (kind, weight) in TransactionTypeWeights)
        {
            cumulative += weight;
            if (draw < cumulative) return kind;
        }
        return TransactionTypeWeights[^1].Kind;
    }

    private static int ExecutionPhase(TransactionKind kind) => kind switch {
        TransactionKind.SubscriptionAddingFund => 0,
        TransactionKind.ManuallyAddingFund => 0,
        TransactionKind.LoyaltyAddingFund => 0,
        TransactionKind.ExpireFund => 1,
        TransactionKind.Payment => 2,
        _ => 2
    };

    private decimal RandomAmount(int min, int max) => Math.Round((decimal)(min + random.NextDouble() * (max - min)), 2);

    private async Task<List<ProjectSeedInfo>> CreateProjectsAsync(int projectCount, DateTime now)
    {
        var projects = new List<ProjectSeedInfo>();

        for (var i = 0; i < projectCount; i++)
        {
            var projectName = $"{SeedMarkerPrefix}{i + 1}";
            var project = new Project { Name = projectName };
            db.Projects.Add(project);
            await db.SaveChangesAsync();

            const string mainGroupName = "VolumeSeed - Groupe principal";
            var mainGroup = new ProductGroup { ProjectId = project.Id, Name = mainGroupName, Color = ProductGroupColor.Color_1, OrderOfAppearance = 1 };
            var loyaltyGroup = new ProductGroup { ProjectId = project.Id, Name = ProductGroupType.LOYALTY, Color = ProductGroupColor.Color_0, OrderOfAppearance = -1 };
            db.ProductGroups.Add(mainGroup);
            db.ProductGroups.Add(loyaltyGroup);

            var beneficiaryType = new BeneficiaryType { ProjectId = project.Id, Name = "VolumeSeed - Type", Keys = "volumeseed" };
            db.BeneficiaryTypes.Add(beneficiaryType);

            var marketName = $"VolumeSeed - Commerce {i + 1}";
            var market = new Market { Name = marketName };
            db.Markets.Add(market);

            // A card can only pay in a market once this join exists (see
            // VerifyCardCanBeUsedInMarket): without it, not a single seeded payment could exist on
            // a real deployment of this project/market pair.
            db.ProjectMarkets.Add(new ProjectMarket { Project = project, Market = market });

            await db.SaveChangesAsync();

            projects.Add(new ProjectSeedInfo {
                Id = project.Id,
                Name = projectName,
                MainProductGroupId = mainGroup.Id,
                MainProductGroupName = mainGroupName,
                LoyaltyProductGroupId = loyaltyGroup.Id,
                LoyaltyProductGroupName = ProductGroupType.LOYALTY,
                BeneficiaryTypeId = beneficiaryType.Id,
                MarketId = market.Id,
                MarketName = marketName
            });
        }

        return projects;
    }

    private async Task<List<OrgSeedInfo>> CreateOrganizationsAsync(List<ProjectSeedInfo> projects, int organizationsTotal)
    {
        var organizations = new List<OrgSeedInfo>();

        for (var i = 0; i < organizationsTotal; i++)
        {
            var projectIndex = i % projects.Count;
            var organization = new Organization { ProjectId = projects[projectIndex].Id, Name = $"VolumeSeed - Organisme {i + 1}" };
            db.Organizations.Add(organization);

            if ((i + 1) % 500 == 0) await db.SaveChangesAsync();

            organizations.Add(new OrgSeedInfo { Id = 0, ProjectIndex = projectIndex, PendingEntity = organization });
        }

        await db.SaveChangesAsync();

        for (var i = 0; i < organizations.Count; i++)
        {
            organizations[i] = organizations[i] with { Id = organizations[i].PendingEntity.Id };
        }

        return organizations;
    }

    private async Task CreateSubscriptionsAsync(List<ProjectSeedInfo> projects, int subscriptionsTotal, DateTime now)
    {
        for (var i = 0; i < subscriptionsTotal; i++)
        {
            var project = projects[i % projects.Count];

            var subscriptionName = $"VolumeSeed - Abonnement {i + 1}";
            var subscription = new Subscription {
                ProjectId = project.Id,
                Name = subscriptionName,
                StartDate = now.AddMonths(-1),
                EndDate = now.AddMonths(2),
                FundsExpirationDate = now.AddMonths(3),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth
            };
            db.Subscriptions.Add(subscription);
            await db.SaveChangesAsync();

            var subscriptionType = new SubscriptionType {
                SubscriptionId = subscription.Id,
                BeneficiaryTypeId = project.BeneficiaryTypeId,
                ProductGroupId = project.MainProductGroupId,
                Amount = 70
            };
            db.SubscriptionTypes.Add(subscriptionType);
            await db.SaveChangesAsync();

            // The first subscription created for a project is the one its funding/expiration
            // transactions reference; later subscriptions for the same project only add volume.
            if (project.SubscriptionId == 0)
            {
                project.SubscriptionId = subscription.Id;
                project.SubscriptionTypeId = subscriptionType.Id;
                project.SubscriptionName = subscriptionName;
            }
        }
    }

    // Creates BudgetAllowance rows attached to organizations (production carries more allowances
    // than organizations, so several can share one). Returns the id of the first allowance created
    // for each organization index, so the SubscriptionBeneficiary rows created afterward can
    // reference one.
    private async Task<Dictionary<int, long>> CreateBudgetAllowancesAsync(List<ProjectSeedInfo> projects, List<OrgSeedInfo> organizations, int budgetAllowancesTotal)
    {
        var firstPerOrganization = new Dictionary<int, BudgetAllowance>();

        for (var i = 0; i < budgetAllowancesTotal; i++)
        {
            var orgIndex = i % organizations.Count;
            var organization = organizations[orgIndex];
            var project = projects[organization.ProjectIndex];

            var originalFund = RandomAmount(100, 500);
            var budgetAllowance = new BudgetAllowance {
                OrganizationId = organization.Id,
                SubscriptionId = project.SubscriptionId,
                OriginalFund = originalFund,
                AvailableFund = originalFund
            };
            db.BudgetAllowances.Add(budgetAllowance);
            firstPerOrganization.TryAdd(orgIndex, budgetAllowance);

            if ((i + 1) % 500 == 0) await db.SaveChangesAsync();
        }

        await db.SaveChangesAsync();

        return firstPerOrganization.ToDictionary(x => x.Key, x => x.Value.Id);
    }

    private void GenerateCardTransactions(
        Card card, Beneficiary beneficiary, OrgSeedInfo organization, ProjectSeedInfo project,
        Fund mainFund, Fund loyaltyFund, int slotCount, DateTime now,
        Dictionary<TransactionKind, int> actualCounts,
        List<(ExpireFundTransaction Expire, AddingFundTransaction Funding)> expirePairs,
        List<(TransactionLog Log, Beneficiary Beneficiary)> pendingLogBeneficiaries,
        SeedStats stats)
    {
        var ledger = new List<AddingFundTransaction>();
        var mainBalance = 0m;
        var loyaltyBalance = 0m;
        var eventTime = now.AddDays(-Math.Max(1, slotCount) * 3);

        var slots = new TransactionKind[slotCount];
        for (var i = 0; i < slotCount; i++)
        {
            var kind = SampleKind();
            if (kind == TransactionKind.LoyaltyAddingFund && loyaltyFund == null) kind = TransactionKind.SubscriptionAddingFund;
            slots[i] = kind;
        }

        // Fundings first, then expirations, then payments: a payment or an expiration needs an
        // existing funding on this same card to draw from.
        var ordered = slots.OrderBy(ExecutionPhase).ToArray();

        foreach (var kind in ordered)
        {
            eventTime = eventTime.AddDays(random.Next(1, 4));
            if (eventTime > now) eventTime = now;

            switch (kind)
            {
                case TransactionKind.LoyaltyAddingFund:
                {
                    var amount = RandomAmount(5, 20);
                    var tx = new LoyaltyAddingFundTransaction {
                        TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(),
                        Amount = amount,
                        AvailableFund = amount,
                        Status = FundTransactionStatus.Actived,
                        ProductGroupId = project.LoyaltyProductGroupId,
                        OrganizationId = organization.Id,
                        ExpirationDate = eventTime.AddMonths(6),
                        CreatedAtUtc = eventTime
                    };
                    tx.Card = card;
                    tx.Beneficiary = beneficiary;
                    db.Transactions.Add(tx);

                    loyaltyBalance += amount;
                    loyaltyFund.Amount = loyaltyBalance;
                    actualCounts[TransactionKind.LoyaltyAddingFund]++;
                    stats.Transactions++;
                    AddTransactionLog(TransactionKind.LoyaltyAddingFund, tx.TransactionUniqueId, amount, eventTime, card, beneficiary, organization, project, pendingLogBeneficiaries, stats);
                    break;
                }

                case TransactionKind.SubscriptionAddingFund:
                case TransactionKind.ManuallyAddingFund:
                {
                    var amount = RandomAmount(20, 100);
                    AddingFundTransaction tx = kind == TransactionKind.SubscriptionAddingFund
                        ? new SubscriptionAddingFundTransaction { SubscriptionTypeId = project.SubscriptionTypeId }
                        : new ManuallyAddingFundTransaction { SubscriptionId = project.SubscriptionId };

                    tx.TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId();
                    tx.Amount = amount;
                    tx.AvailableFund = amount;
                    tx.Status = FundTransactionStatus.Actived;
                    tx.ProductGroupId = project.MainProductGroupId;
                    tx.OrganizationId = organization.Id;
                    tx.ExpirationDate = eventTime.AddMonths(3);
                    tx.CreatedAtUtc = eventTime;
                    tx.Card = card;
                    tx.Beneficiary = beneficiary;
                    db.Transactions.Add(tx);

                    ledger.Add(tx);
                    mainBalance += amount;
                    mainFund.Amount = mainBalance;
                    actualCounts[kind]++;
                    stats.Transactions++;
                    AddTransactionLog(kind, tx.TransactionUniqueId, amount, eventTime, card, beneficiary, organization, project, pendingLogBeneficiaries, stats);
                    break;
                }

                case TransactionKind.ExpireFund:
                {
                    var entry = ledger.FirstOrDefault(e => e.AvailableFund > 0);
                    if (entry == null) goto case TransactionKind.SubscriptionAddingFund;

                    var remaining = entry.AvailableFund;
                    var expireTx = new ExpireFundTransaction {
                        TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(),
                        Amount = remaining,
                        ProductGroupId = project.MainProductGroupId,
                        OrganizationId = organization.Id,
                        ExpiredSubscriptionId = project.SubscriptionId,
                        CreatedAtUtc = eventTime
                    };
                    expireTx.Card = card;
                    expireTx.Beneficiary = beneficiary;
                    expireTx.AddingFundTransaction = entry;
                    db.Transactions.Add(expireTx);
                    expirePairs.Add((expireTx, entry));

                    entry.AvailableFund = 0;
                    entry.Status = FundTransactionStatus.Expired;
                    mainBalance -= remaining;
                    mainFund.Amount = mainBalance;
                    actualCounts[TransactionKind.ExpireFund]++;
                    stats.Transactions++;
                    AddTransactionLog(TransactionKind.ExpireFund, expireTx.TransactionUniqueId, remaining, eventTime, card, beneficiary, organization, project, pendingLogBeneficiaries, stats);
                    break;
                }

                case TransactionKind.Payment:
                {
                    var entry = ledger.FirstOrDefault(e => e.AvailableFund > 0);
                    if (entry == null) goto case TransactionKind.SubscriptionAddingFund;

                    var amount = Math.Min(RandomAmount(5, 50), entry.AvailableFund);
                    var paymentTx = new PaymentTransaction {
                        TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(),
                        Amount = amount,
                        MarketId = project.MarketId,
                        OrganizationId = organization.Id,
                        CreatedAtUtc = eventTime,
                        Transactions = new List<AddingFundTransaction> { entry },
                        PaymentTransactionAddingFundTransactions = new List<PaymentTransactionAddingFundTransaction> {
                            new() { AddingFundTransaction = entry, Amount = amount, RefundAmount = 0 }
                        },
                        TransactionByProductGroups = new List<PaymentTransactionProductGroup> {
                            new() { ProductGroupId = project.MainProductGroupId, Amount = amount, RefundAmount = 0 }
                        }
                    };
                    paymentTx.Card = card;
                    paymentTx.Beneficiary = beneficiary;
                    db.Transactions.Add(paymentTx);

                    entry.AvailableFund -= amount;
                    mainBalance -= amount;
                    mainFund.Amount = mainBalance;
                    actualCounts[TransactionKind.Payment]++;
                    stats.Transactions++;
                    AddTransactionLog(TransactionKind.Payment, paymentTx.TransactionUniqueId, amount, eventTime, card, beneficiary, organization, project, pendingLogBeneficiaries, stats);
                    break;
                }
            }
        }
    }

    // TransactionLog is the largest table in production (421 MB) and the one
    // ReportService/dashboards actually read from. It has no EF relationship to Beneficiary (a
    // flat, denormalized snapshot instead), so the caller fills in BeneficiaryId once the
    // beneficiary has been saved and has a real id.
    private void AddTransactionLog(
        TransactionKind kind, string transactionUniqueId, decimal amount, DateTime eventTime,
        Card card, Beneficiary beneficiary, OrgSeedInfo organization, ProjectSeedInfo project,
        List<(TransactionLog Log, Beneficiary Beneficiary)> pendingLogBeneficiaries, SeedStats stats)
    {
        var discriminator = kind switch {
            TransactionKind.Payment => TransactionLogDiscriminator.PaymentTransactionLog,
            TransactionKind.SubscriptionAddingFund => TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog,
            TransactionKind.ExpireFund => TransactionLogDiscriminator.ExpireFundTransactionLog,
            TransactionKind.ManuallyAddingFund => TransactionLogDiscriminator.ManuallyAddingFundTransactionLog,
            TransactionKind.LoyaltyAddingFund => TransactionLogDiscriminator.LoyaltyAddingFundTransactionLog,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };

        var isLoyalty = kind == TransactionKind.LoyaltyAddingFund;
        var isPayment = kind == TransactionKind.Payment;
        var hasSubscription = kind is TransactionKind.SubscriptionAddingFund or TransactionKind.ManuallyAddingFund or TransactionKind.ExpireFund;

        var log = new TransactionLog {
            TransactionUniqueId = transactionUniqueId,
            CreatedAtUtc = eventTime,
            Discriminator = discriminator,
            TotalAmount = amount,
            CardProgramCardId = card.ProgramCardId,
            CardNumber = card.CardNumber,
            BeneficiaryFirstname = beneficiary.Firstname,
            BeneficiaryLastname = beneficiary.Lastname,
            BeneficiaryTypeId = project.BeneficiaryTypeId,
            OrganizationId = organization.Id,
            OrganizationName = organization.PendingEntity.Name,
            ProjectId = project.Id,
            ProjectName = project.Name,
            MarketId = isPayment ? project.MarketId : null,
            MarketName = isPayment ? project.MarketName : null,
            SubscriptionId = hasSubscription ? project.SubscriptionId : null,
            SubscriptionName = hasSubscription ? project.SubscriptionName : null,
            TransactionLogProductGroups = new List<TransactionLogProductGroup> {
                new() {
                    ProductGroupId = isLoyalty ? project.LoyaltyProductGroupId : project.MainProductGroupId,
                    ProductGroupName = isLoyalty ? project.LoyaltyProductGroupName : project.MainProductGroupName,
                    Amount = amount
                }
            }
        };
        db.TransactionLogs.Add(log);
        pendingLogBeneficiaries.Add((log, beneficiary));
        stats.TransactionLogs++;
    }

    private void LogSummary(decimal scale, SeedStats stats, Dictionary<TransactionKind, int> actualTransactionCounts)
    {
        var total = Math.Max(1, stats.Transactions);
        var breakdown = string.Join(", ", actualTransactionCounts
            .OrderByDescending(x => x.Value)
            .Select(x => $"{x.Key}: {x.Value} ({100m * x.Value / total:0.0}%)"));

        logger.LogInformation(
            $"[VolumeDataSeeder] Seed done (scale: {scale}) - " +
            $"Organizations: {stats.Organizations}, Participants: {stats.Participants}, Cards: {stats.Cards}, " +
            $"Funds: {stats.Funds}, Subscriptions: {stats.Subscriptions}, " +
            $"SubscriptionBeneficiaries: {stats.SubscriptionBeneficiaries}, BudgetAllowances: {stats.BudgetAllowances}, " +
            $"Transactions: {stats.Transactions} [{breakdown}], TransactionLogs: {stats.TransactionLogs}");
    }

    // Thrown instead of silently skipping when the database already carries VolumeSeed data,
    // complete or partial: a marker saved before the rest of the dataset exists (the first project
    // created) would otherwise let an interrupted run look finished on the next restart.
    public class NonEmptyDatabaseException : Exception
    {
        public NonEmptyDatabaseException(string message) : base(message) { }
    }

    private class ProjectSeedInfo
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public long MainProductGroupId { get; set; }
        public string MainProductGroupName { get; set; }
        public long LoyaltyProductGroupId { get; set; }
        public string LoyaltyProductGroupName { get; set; }
        public long BeneficiaryTypeId { get; set; }
        public long MarketId { get; set; }
        public string MarketName { get; set; }
        public long SubscriptionId { get; set; }
        public long SubscriptionTypeId { get; set; }
        public string SubscriptionName { get; set; }
    }

    private record OrgSeedInfo
    {
        public long Id { get; init; }
        public int ProjectIndex { get; init; }
        public Organization PendingEntity { get; init; }
    }

    private class SeedStats
    {
        public int Organizations { get; set; }
        public int Participants { get; set; }
        public int Cards { get; set; }
        public int Funds { get; set; }
        public int Subscriptions { get; set; }
        public int SubscriptionBeneficiaries { get; set; }
        public int BudgetAllowances { get; set; }
        public int Transactions { get; set; }
        public int TransactionLogs { get; set; }
    }
}
