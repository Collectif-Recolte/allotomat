using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Sig.App.Backend.DataSeeders;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.DataSeeders
{
    public class VolumeDataSeederTest : TestBase
    {
        // Production baselines the ticket names, copied here independently of
        // VolumeDataSeeder's own constants so a mutation of its formula cannot also mutate what
        // the test expects.
        private const int BaselineOrganizations = 318;
        private const int BaselineParticipants = 21511;
        private const int BaselineCards = 33610;
        private const int BaselineFunds = 36473;
        private const int BaselineTransactions = 317250;
        private const int BaselineSubscriptions = 109;
        private const int BaselineBudgetAllowances = 567;
        private const int MaxProjects = 8;

        // The seeder parses VolumeSeed:Scale with InvariantCulture, so the configuration value a
        // test feeds it has to be written the same way. decimal.ToString() follows the machine's
        // culture instead: on a French-locale developer machine 0.05m renders as "0,05", the parse
        // fails, and the seeder skips silently, so the test fails there and only there.
        private static string Config(decimal scale) => scale.ToString(CultureInfo.InvariantCulture);

        private static int Expected(int baseline, decimal scale) =>
            Math.Max(1, (int)Math.Round(baseline * scale, MidpointRounding.AwayFromZero));

        // The seeder guarantees at least one subscription per project, so at a small scale the
        // floor (not the raw percentage) can be what actually governs the count.
        private static int ExpectedSubscriptions(decimal scale)
        {
            var projectCount = Math.Clamp(Expected(BaselineOrganizations, scale), 1, MaxProjects);
            return Math.Max(projectCount, Expected(BaselineSubscriptions, scale));
        }

        private static int ExpectedProjectCount(decimal scale) =>
            Math.Clamp(Expected(BaselineOrganizations, scale), 1, MaxProjects);

        // Transaction-type shares measured in production (resultats-a08-volumetrie-2026-08-28.md),
        // independent of VolumeDataSeeder's own TransactionTypeWeights so a mutation of its formula
        // cannot also mutate what the test expects.
        private const decimal PaymentShareTarget = 0.4602m;
        private const decimal SubscriptionShareTarget = 0.4600m;
        private const decimal ExpireShareTarget = 0.0528m;
        private const decimal ManualShareTarget = 0.0234m;
        private const decimal LoyaltyShareTarget = 0.0028m;

        // Relative, not a fixed number of percentage points: an absolute band wide enough to
        // survive the sampling noise on a 46% type would let a rare type like loyalty (0.28%) drop
        // to zero without leaving that band, while a band tight enough to catch that would be far
        // too strict for the 46% types. +/-50% keeps every type's realized share tied to its own
        // target, and also covers the seeder's redirect of a ledger-empty payment/expiration draw
        // into a subscription-adding-fund transaction, which shifts that share up by a few points.
        // A distribution flattened toward one type, or missing one entirely, still falls well
        // outside this band.
        private const decimal ShareRelativeTolerance = 0.5m;

        private static void ShareShouldBeNear(int count, int total, decimal targetShare, string transactionKind)
        {
            var actualShare = total == 0 ? 0m : (decimal)count / total;
            var minShare = targetShare * (1 - ShareRelativeTolerance);
            var maxShare = targetShare * (1 + ShareRelativeTolerance);

            actualShare.Should().BeInRange(minShare, maxShare,
                $"{transactionKind} should stay near its production share of {targetShare:P2}, not merely be present");
        }

        private static AppDbContext CreateIndependentDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .EnableDetailedErrors()
                .Options;
            return new AppDbContext(options);
        }

        private static IWebHostEnvironment CreateEnvironment(string name)
        {
            var mock = new Mock<IWebHostEnvironment>();
            mock.Setup(x => x.EnvironmentName).Returns(name);
            return mock.Object;
        }

        private VolumeDataSeeder CreateSeeder(string scale, AppDbContext dbContext = null, string environmentName = "Development")
        {
            var settings = scale == null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["VolumeSeed:Scale"] = scale };

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

            return new VolumeDataSeeder(dbContext ?? DbContext, Logger<VolumeDataSeeder>(), Clock, configuration, CreateEnvironment(environmentName));
        }

        [Fact]
        public async Task Seed_WithoutScaleConfigured_CreatesNothing()
        {
            var seeder = CreateSeeder(scale: null);

            await seeder.Seed();

            (await DbContext.Projects.CountAsync()).Should().Be(0);
            (await DbContext.Organizations.CountAsync()).Should().Be(0);
            (await DbContext.Beneficiaries.CountAsync()).Should().Be(0);
            (await DbContext.Cards.CountAsync()).Should().Be(0);
            (await DbContext.Transactions.CountAsync()).Should().Be(0);
        }

        [Theory]
        [InlineData("Staging")]
        [InlineData("UAT")]
        [InlineData("QA")]
        [InlineData("Production")]
        public async Task Seed_OutsideDevelopmentEnvironment_CreatesNothingEvenWithScaleConfigured(string environmentName)
        {
            // A key testing the environment name against "not Production" instead of "is
            // Development" would let a UAT, QA or Staging environment run this seeder, on a
            // database shared by testers, if the configuration key were ever set there by mistake.
            var seeder = CreateSeeder(scale: "0.05", environmentName: environmentName);

            await seeder.Seed();

            (await DbContext.Projects.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task Seed_WithScaleConfigured_ScalesEveryEntityByThatFactor()
        {
            const decimal scale = 0.05m;
            var seeder = CreateSeeder(Config(scale));

            await seeder.Seed();

            (await DbContext.Organizations.CountAsync()).Should().Be(Expected(BaselineOrganizations, scale));
            (await DbContext.Beneficiaries.CountAsync()).Should().Be(Expected(BaselineParticipants, scale));
            (await DbContext.Cards.CountAsync()).Should().Be(Expected(BaselineCards, scale));
            (await DbContext.Subscriptions.CountAsync()).Should().Be(ExpectedSubscriptions(scale));
            (await DbContext.SubscriptionBeneficiaries.CountAsync()).Should().Be(Expected(BaselineParticipants, scale));
            (await DbContext.BudgetAllowances.CountAsync()).Should().Be(Expected(BaselineBudgetAllowances, scale));
            (await DbContext.ProjectMarkets.CountAsync()).Should().Be(ExpectedProjectCount(scale));
            (await DbContext.Transactions.CountAsync()).Should().Be(Expected(BaselineTransactions, scale));
            (await DbContext.TransactionLogs.CountAsync()).Should().Be(Expected(BaselineTransactions, scale));
        }

        [Fact]
        public async Task Seed_WithATenthOfTheScale_ProducesRoughlyATenthOfTheVolume()
        {
            const decimal fullScale = 0.05m;
            const decimal tenthScale = 0.005m;

            // Two entirely separate databases: TestBase's shared DbContext would make the second
            // Seed() see the first run's data and refuse as an already-seeded dataset.
            using var fullDbContext = CreateIndependentDbContext();
            var fullSeeder = CreateSeeder(Config(fullScale), fullDbContext);
            await fullSeeder.Seed();
            var fullTransactionCount = await fullDbContext.Transactions.CountAsync();
            var fullFundCount = await fullDbContext.Funds.CountAsync();

            using var tenthDbContext = CreateIndependentDbContext();
            var tenthSeeder = CreateSeeder(Config(tenthScale), tenthDbContext);
            await tenthSeeder.Seed();
            var tenthTransactionCount = await tenthDbContext.Transactions.CountAsync();
            var tenthFundCount = await tenthDbContext.Funds.CountAsync();

            tenthTransactionCount.Should().Be(Expected(BaselineTransactions, tenthScale));
            fullTransactionCount.Should().Be(Expected(BaselineTransactions, fullScale));
            tenthFundCount.Should().Be(Expected(BaselineFunds, tenthScale));
            fullFundCount.Should().Be(Expected(BaselineFunds, fullScale));
        }

        [Fact]
        public async Task Seed_AtASufficientScale_ProducesEveryTransactionTypeFromTheProductionDistribution()
        {
            // A larger scale than the other per-relation tests here: the rarest type (loyalty, a
            // 0.28% target) needs enough draws that its realized share is not dominated by noise
            // from a single flipped sample.
            var seeder = CreateSeeder(scale: "0.1");

            await seeder.Seed();

            var total = await DbContext.Transactions.CountAsync();
            var payment = await DbContext.Transactions.OfType<PaymentTransaction>().CountAsync();
            var subscription = await DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().CountAsync();
            var expire = await DbContext.Transactions.OfType<ExpireFundTransaction>().CountAsync();
            var manual = await DbContext.Transactions.OfType<ManuallyAddingFundTransaction>().CountAsync();
            var loyalty = await DbContext.Transactions.OfType<LoyaltyAddingFundTransaction>().CountAsync();

            ShareShouldBeNear(payment, total, PaymentShareTarget, "payments");
            ShareShouldBeNear(subscription, total, SubscriptionShareTarget, "subscription adding-fund");
            ShareShouldBeNear(expire, total, ExpireShareTarget, "expirations");
            ShareShouldBeNear(manual, total, ManualShareTarget, "manual adding-fund");
            ShareShouldBeNear(loyalty, total, LoyaltyShareTarget, "loyalty adding-fund");
        }

        // Distinct from the distribution test above, which tolerates a +/-50% band per type and
        // would not notice a transaction created under one CLR type while its TransactionLog is
        // written under another discriminator: a ledger-empty Payment/ExpireFund redirect that
        // forgets to update its own "kind" does exactly that, creating a ManuallyAddingFundTransaction
        // while still logging it as the original Payment or ExpireFund kind. Pairing each entity
        // type with its own log discriminator, with no tolerance, catches that mismatch directly.
        [Fact]
        public async Task Seed_AtASufficientScale_CreatesTheSameTransactionTypeItLogs()
        {
            var seeder = CreateSeeder(scale: "0.1");

            await seeder.Seed();

            var paymentCount = await DbContext.Transactions.OfType<PaymentTransaction>().CountAsync();
            var subscriptionCount = await DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().CountAsync();
            var expireCount = await DbContext.Transactions.OfType<ExpireFundTransaction>().CountAsync();
            var manualCount = await DbContext.Transactions.OfType<ManuallyAddingFundTransaction>().CountAsync();
            var loyaltyCount = await DbContext.Transactions.OfType<LoyaltyAddingFundTransaction>().CountAsync();

            var paymentLogCount = await DbContext.TransactionLogs.CountAsync(x => x.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog);
            var subscriptionLogCount = await DbContext.TransactionLogs.CountAsync(x => x.Discriminator == TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog);
            var expireLogCount = await DbContext.TransactionLogs.CountAsync(x => x.Discriminator == TransactionLogDiscriminator.ExpireFundTransactionLog);
            var manualLogCount = await DbContext.TransactionLogs.CountAsync(x => x.Discriminator == TransactionLogDiscriminator.ManuallyAddingFundTransactionLog);
            var loyaltyLogCount = await DbContext.TransactionLogs.CountAsync(x => x.Discriminator == TransactionLogDiscriminator.LoyaltyAddingFundTransactionLog);

            paymentCount.Should().Be(paymentLogCount,
                "every PaymentTransaction row must be announced by a PaymentTransactionLog, not some other kind");
            subscriptionCount.Should().Be(subscriptionLogCount,
                "every SubscriptionAddingFundTransaction row must be announced by a SubscriptionAddingFundTransactionLog, not some other kind");
            expireCount.Should().Be(expireLogCount,
                "every ExpireFundTransaction row must be announced by an ExpireFundTransactionLog, not some other kind");
            manualCount.Should().Be(manualLogCount,
                "every ManuallyAddingFundTransaction row must be announced by a ManuallyAddingFundTransactionLog, not some other kind");
            loyaltyCount.Should().Be(loyaltyLogCount,
                "every LoyaltyAddingFundTransaction row must be announced by a LoyaltyAddingFundTransactionLog, not some other kind");
        }

        [Fact]
        public async Task Seed_ProducesCardsAndFundsThatBelongToARealParticipantAndProductGroup()
        {
            var seeder = CreateSeeder(scale: "0.01");

            await seeder.Seed();

            var cards = await DbContext.Cards.Include(x => x.Beneficiary).ToListAsync();
            cards.Should().NotBeEmpty();
            cards.Where(x => x.Beneficiary != null).Should().OnlyContain(x => x.Beneficiary.OrganizationId > 0);

            var funds = await DbContext.Funds.ToListAsync();
            funds.Should().NotBeEmpty();
            funds.Should().OnlyContain(x => x.CardId != null && x.ProductGroupId > 0);

            var payments = await DbContext.Transactions.OfType<PaymentTransaction>().ToListAsync();
            payments.Should().NotBeEmpty();
            var joins = await DbContext.PaymentTransactionAddingFundTransactions.ToListAsync();
            joins.Should().NotBeEmpty();
            joins.Should().OnlyContain(x => x.AddingFundTransactionId > 0 && x.PaymentTransactionId > 0);
        }

        [Fact]
        public async Task Seed_LinksEveryProjectToItsMarketThroughProjectMarket()
        {
            var seeder = CreateSeeder(scale: "0.01");

            await seeder.Seed();

            var projectCount = await DbContext.Projects.CountAsync();
            var projectMarkets = await DbContext.ProjectMarkets.ToListAsync();

            projectMarkets.Should().HaveCount(projectCount,
                "VerifyCardCanBeUsedInMarket requires a ProjectMarket row before a payment can exist on a project/market pair");

            var payments = await DbContext.Transactions.OfType<PaymentTransaction>().ToListAsync();
            payments.Should().NotBeEmpty();

            var cardProjectById = await DbContext.Cards.ToDictionaryAsync(x => x.Id, x => x.ProjectId);
            foreach (var payment in payments)
            {
                var projectId = cardProjectById[payment.CardId.Value];
                projectMarkets.Should().Contain(x => x.ProjectId == projectId && x.MarketId == payment.MarketId,
                    "every seeded payment must sit on a project/market pair that is actually linked");
            }
        }

        [Fact]
        public async Task Seed_CreatesSubscriptionBeneficiariesTheAddingFundJobCanIterate()
        {
            var seeder = CreateSeeder(scale: "0.01");

            await seeder.Seed();

            var subscriptions = await DbContext.Subscriptions.Include(x => x.Beneficiaries).ToListAsync();

            subscriptions.SelectMany(x => x.Beneficiaries).Should().NotBeEmpty(
                "AddingFundToCard iterates exactly Subscription.Beneficiaries; without any, the monthly adding-fund job would have no participant to pay");
        }

        [Fact]
        public async Task Seed_CreatesBudgetAllowancesTheAddingFundJobCanIterate()
        {
            var seeder = CreateSeeder(scale: "0.01");

            await seeder.Seed();

            var subscriptions = await DbContext.Subscriptions.Include(x => x.BudgetAllowances).ToListAsync();

            subscriptions.SelectMany(x => x.BudgetAllowances).Should().NotBeEmpty(
                "AddingFundToCard also iterates Subscription.BudgetAllowances; without any, the monthly adding-fund job would have nothing to process");
        }

        [Fact]
        public async Task Seed_CreatesOneTransactionLogPerTransaction()
        {
            // At scale 0.02 every participant fits in the seeder's first (and only) 500-row batch,
            // so checking BeneficiaryId across all logs would exercise nothing that checking just
            // one log would not. 0.05 produces over 1000 participants, several batches, so the
            // assertion below actually exercises attachment past the first batch.
            var seeder = CreateSeeder(scale: "0.05");

            await seeder.Seed();

            var transactionCount = await DbContext.Transactions.CountAsync();
            var transactionLogCount = await DbContext.TransactionLogs.CountAsync();

            transactionLogCount.Should().Be(transactionCount,
                "TransactionLogs is the largest table in production (421 MB); a dataset without it does not exercise anything reading from it");
            (await DbContext.TransactionLogs.AllAsync(x => x.BeneficiaryId != null && x.BeneficiaryId > 0)).Should().BeTrue(
                "TransactionLog rows are snapshot data with no EF relationship, so BeneficiaryId must be filled in explicitly, on every row and every batch, once the participant has a real id");
        }

        [Fact]
        public async Task Seed_WhenDatabaseAlreadyCarriesACompleteVolumeDataset_RefusesAndThrows()
        {
            var seeder = CreateSeeder(scale: "0.01");
            await seeder.Seed();

            var secondSeeder = CreateSeeder(scale: "0.01");

            await F(() => secondSeeder.Seed()).Should().ThrowAsync<VolumeDataSeeder.NonEmptyDatabaseException>();
        }

        [Fact]
        public async Task Seed_WhenDatabaseCarriesOnlyAPartialVolumeDataset_RefusesAndThrows()
        {
            // Simulates a run interrupted right after its very first write: a single Project row
            // exists and nothing else does. The seeder must refuse just as loudly as it would on a
            // complete dataset: silently treating this as "already seeded" would hide the fact that
            // the run never produced any usable data.
            DbContext.Projects.Add(new Project { Name = "VolumeSeed - Programme 1" });
            await DbContext.SaveChangesAsync();

            var seeder = CreateSeeder(scale: "0.01");

            await F(() => seeder.Seed()).Should().ThrowAsync<VolumeDataSeeder.NonEmptyDatabaseException>();
        }

        [Fact]
        public async Task Seed_WhenDatabaseCarriesTheRegularDevelopmentSeed_RefusesAndThrows()
        {
            // The realistic case: a developer runs the app once normally, DevDataSeeder fills the
            // database, then they set VolumeSeed:Scale and restart. A guard keyed on this seeder's
            // own project name would not recognize "SeedDev - Programme 1" and would append the
            // volume dataset on top of it, giving a mixed database whose measured volumes mean
            // nothing.
            DbContext.Projects.Add(new Project { Name = "SeedDev - Programme 1" });
            await DbContext.SaveChangesAsync();

            var seeder = CreateSeeder(scale: "0.01");

            await F(() => seeder.Seed()).Should().ThrowAsync<VolumeDataSeeder.NonEmptyDatabaseException>();
            (await DbContext.Projects.CountAsync()).Should().Be(1, "the refusal must leave the existing database untouched");
            (await DbContext.Organizations.CountAsync()).Should().Be(0);
        }
    }
}
