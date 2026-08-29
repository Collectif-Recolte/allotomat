using FluentAssertions;
using GraphQL.Conventions;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Requests.Queries.Organizations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Queries.Organizations
{
    // Characterization tests for GetOrganizationsStats.Handle(): they pin the handler's current
    // behaviour, defect included, and must keep passing unchanged once the handler is rewritten.
    // A numeric mismatch means the rewrite is wrong, not the test.
    public class GetOrganizationsStatsTest : TestBase
    {
        private readonly GetOrganizationsStats handler;

        private readonly Project project1;

        private readonly Organization organizationWithoutTransactions;
        private readonly Organization organizationCardAndBranches;
        private readonly Organization organizationArchivedExclusion;
        private readonly Organization organizationSubscriptionFilter;
        private readonly Organization organizationEnvelopePrecedence;
        private readonly Organization organizationOtherProject;

        private readonly Subscription subscriptionKept;
        private readonly Subscription subscriptionDropped;
        private readonly Subscription subscriptionFilterOne;
        private readonly Subscription subscriptionFilterTwo;
        private readonly Subscription subscriptionFilterArchived;
        private readonly Subscription subscriptionArchivedFutureExpiration;
        private readonly Subscription subscriptionArchivedPastExpiration;

        public GetOrganizationsStatsTest()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            project1 = new Project { Name = "Project 1" };
            var project2 = new Project { Name = "Project 2" };
            DbContext.Projects.AddRange(project1, project2);

            var productGroup = new ProductGroup { Name = "Product group", Color = ProductGroupColor.Color_1, OrderOfAppearance = 1, Project = project1 };
            var productGroupOtherProject = new ProductGroup { Name = "Product group (other project)", Color = ProductGroupColor.Color_1, OrderOfAppearance = 1, Project = project2 };
            DbContext.ProductGroups.AddRange(productGroup, productGroupOtherProject);

            // Fact 7: an organization with no transaction at all must still be in the payload, zeroed out.
            organizationWithoutTransactions = new Organization { Name = "Organization without transactions", Project = project1 };

            // Facts 1, 2, 3: CardId filtering on BalanceOnCards/TotalAllocatedOnCards versus
            // CardSpendingAmounts, and the three status branches of CardSpendingAmounts.
            organizationCardAndBranches = new Organization { Name = "Organization card and branches", Project = project1 };

            // Facts 4, 9: an archived subscription's lines are excluded, with and without the filter.
            organizationArchivedExclusion = new Organization { Name = "Organization archived exclusion", Project = project1 };

            // Fact 5: the Subscriptions filter restricts subscription-adding-fund transactions by
            // SubscriptionType.SubscriptionId.
            organizationSubscriptionFilter = new Organization { Name = "Organization subscription filter", Project = project1 };

            // Fact 6: pre-existing envelope precedence defect, reproduced on purpose.
            organizationEnvelopePrecedence = new Organization { Name = "Organization envelope precedence", Project = project1 };

            // Fact 8: a second project whose transactions must not contribute to project1's totals.
            organizationOtherProject = new Organization { Name = "Organization other project", Project = project2 };

            DbContext.Organizations.AddRange(
                organizationWithoutTransactions,
                organizationCardAndBranches,
                organizationArchivedExclusion,
                organizationSubscriptionFilter,
                organizationEnvelopePrecedence,
                organizationOtherProject);

            // --- Facts 1, 2, 3: card-id filtering and the three CardSpendingAmounts branches -----

            var subscriptionCardAndBranches = new Subscription
            {
                Name = "Subscription card and branches",
                Project = project1,
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                FundsExpirationDate = today.AddDays(-30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsFundsAccumulable = false,
                IsArchived = false
            };
            DbContext.Subscriptions.Add(subscriptionCardAndBranches);

            var cardWithFunds = new Card { Project = project1, Status = CardStatus.Assigned, Funds = new List<Fund>(), Transactions = new List<Transaction>() };
            DbContext.Cards.Add(cardWithFunds);

            // Active: Amount - AvailableFund = 100 - 30 = 70. Has a card.
            var activeWithCard = new ManuallyAddingFundTransaction
            {
                Organization = organizationCardAndBranches,
                Subscription = subscriptionCardAndBranches,
                ProductGroup = productGroup,
                Card = cardWithFunds,
                ExpirationDate = today.AddMonths(1),
                Status = FundTransactionStatus.Actived,
                Amount = 100,
                AvailableFund = 30
            };

            // Expired with an ExpireFundTransaction: Amount - ExpireFundTransaction.Amount = 200 - 50 = 150.
            // No card: this is the line that CardSpendingAmounts must include despite CardId being null
            // (fact 2), unlike BalanceOnCards/TotalAllocatedOnCards (fact 1).
            var expiredWithEftNoCard = new ManuallyAddingFundTransaction
            {
                Organization = organizationCardAndBranches,
                Subscription = subscriptionCardAndBranches,
                ProductGroup = productGroup,
                Card = null,
                ExpirationDate = today.AddMonths(-1),
                Status = FundTransactionStatus.Expired,
                Amount = 200,
                AvailableFund = 999 // must be ignored in favour of the ExpireFundTransaction's own amount
            };

            // Expired without an ExpireFundTransaction: falls back to Amount - AvailableFund = 80 - 20 = 60.
            var expiredNoEftWithCard = new ManuallyAddingFundTransaction
            {
                Organization = organizationCardAndBranches,
                Subscription = subscriptionCardAndBranches,
                ProductGroup = productGroup,
                Card = cardWithFunds,
                ExpirationDate = today.AddMonths(-1),
                Status = FundTransactionStatus.Expired,
                Amount = 80,
                AvailableFund = 20
            };
            DbContext.Transactions.AddRange(activeWithCard, expiredWithEftNoCard, expiredNoEftWithCard);

            var expireFundTransaction = new ExpireFundTransaction
            {
                AddingFundTransaction = expiredWithEftNoCard,
                ExpiredSubscription = subscriptionCardAndBranches,
                Organization = organizationCardAndBranches,
                ProductGroup = productGroup,
                Amount = 50
            };
            expiredWithEftNoCard.ExpireFundTransaction = expireFundTransaction;
            DbContext.Transactions.Add(expireFundTransaction);

            // --- Facts 4, 9: archived subscription exclusion, with and without the filter ---------

            subscriptionKept = new Subscription
            {
                Name = "Subscription kept",
                Project = project1,
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                FundsExpirationDate = today.AddDays(-30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsFundsAccumulable = false,
                IsArchived = false
            };
            subscriptionDropped = new Subscription
            {
                Name = "Subscription dropped (archived)",
                Project = project1,
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                FundsExpirationDate = today.AddDays(-30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsFundsAccumulable = false,
                IsArchived = true
            };
            DbContext.Subscriptions.AddRange(subscriptionKept, subscriptionDropped);

            // Kept subscription: an active line (40 - 15 = 25) plus an expired line with its own
            // expiration transaction (10 - 4 = 6). CardSpendingAmounts contribution: 31. Its expiration
            // transaction's amount (4) is the only one that must reach ExpiredAmounts.
            var manualKept = new ManuallyAddingFundTransaction
            {
                Organization = organizationArchivedExclusion,
                Subscription = subscriptionKept,
                ProductGroup = productGroup,
                ExpirationDate = today.AddMonths(1),
                Status = FundTransactionStatus.Actived,
                Amount = 40,
                AvailableFund = 15
            };
            var expireSourceKept = new ManuallyAddingFundTransaction
            {
                Organization = organizationArchivedExclusion,
                Subscription = subscriptionKept,
                ProductGroup = productGroup,
                ExpirationDate = today.AddMonths(-1),
                Status = FundTransactionStatus.Expired,
                Amount = 10,
                AvailableFund = 0
            };

            // Dropped (archived) subscription: same shapes, much larger amounts, so any leak is obvious.
            // Must contribute zero everywhere, whether or not the filter names it explicitly.
            var manualDropped = new ManuallyAddingFundTransaction
            {
                Organization = organizationArchivedExclusion,
                Subscription = subscriptionDropped,
                ProductGroup = productGroup,
                ExpirationDate = today.AddMonths(1),
                Status = FundTransactionStatus.Actived,
                Amount = 777,
                AvailableFund = 333
            };
            var expireSourceDropped = new ManuallyAddingFundTransaction
            {
                Organization = organizationArchivedExclusion,
                Subscription = subscriptionDropped,
                ProductGroup = productGroup,
                ExpirationDate = today.AddMonths(-1),
                Status = FundTransactionStatus.Expired,
                Amount = 888,
                AvailableFund = 0
            };
            DbContext.Transactions.AddRange(manualKept, expireSourceKept, manualDropped, expireSourceDropped);

            var expireTxKept = new ExpireFundTransaction
            {
                AddingFundTransaction = expireSourceKept,
                ExpiredSubscription = subscriptionKept,
                Organization = organizationArchivedExclusion,
                ProductGroup = productGroup,
                Amount = 4
            };
            expireSourceKept.ExpireFundTransaction = expireTxKept;

            var expireTxDropped = new ExpireFundTransaction
            {
                AddingFundTransaction = expireSourceDropped,
                ExpiredSubscription = subscriptionDropped,
                Organization = organizationArchivedExclusion,
                ProductGroup = productGroup,
                Amount = 444
            };
            expireSourceDropped.ExpireFundTransaction = expireTxDropped;
            DbContext.Transactions.AddRange(expireTxKept, expireTxDropped);

            // --- Fact 5: the Subscriptions filter on subscription-adding-fund transactions --------

            subscriptionFilterOne = new Subscription
            {
                Name = "Subscription filter one",
                Project = project1,
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                FundsExpirationDate = today.AddDays(-30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsFundsAccumulable = false,
                IsArchived = false
            };
            subscriptionFilterTwo = new Subscription
            {
                Name = "Subscription filter two",
                Project = project1,
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                FundsExpirationDate = today.AddDays(-30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsFundsAccumulable = false,
                IsArchived = false
            };
            DbContext.Subscriptions.AddRange(subscriptionFilterOne, subscriptionFilterTwo);

            var subscriptionTypeOne = new SubscriptionType { Subscription = subscriptionFilterOne, ProductGroup = productGroup, Amount = 10 };
            var subscriptionTypeTwo = new SubscriptionType { Subscription = subscriptionFilterTwo, ProductGroup = productGroup, Amount = 10 };
            DbContext.SubscriptionTypes.AddRange(subscriptionTypeOne, subscriptionTypeTwo);

            // 60 - 25 = 35, on subscriptionFilterOne.
            var subscriptionTxOne = new SubscriptionAddingFundTransaction
            {
                Organization = organizationSubscriptionFilter,
                SubscriptionType = subscriptionTypeOne,
                ProductGroup = productGroup,
                ExpirationDate = today.AddMonths(1),
                Status = FundTransactionStatus.Actived,
                Amount = 60,
                AvailableFund = 25
            };
            // 90 - 40 = 50, on subscriptionFilterTwo. Must disappear once the filter names only
            // subscriptionFilterOne, even though subscriptionFilterTwo is not archived.
            var subscriptionTxTwo = new SubscriptionAddingFundTransaction
            {
                Organization = organizationSubscriptionFilter,
                SubscriptionType = subscriptionTypeTwo,
                ProductGroup = productGroup,
                ExpirationDate = today.AddMonths(1),
                Status = FundTransactionStatus.Actived,
                Amount = 90,
                AvailableFund = 40
            };
            DbContext.Transactions.AddRange(subscriptionTxOne, subscriptionTxTwo);

            // Fact 4, applied to SubscriptionAddingFundTransaction: an archived subscription's line
            // must be excluded in both branches (":54" without the filter, ":83" restated identically
            // in the filtered branch), the same way it already is for ManuallyAddingFundTransaction in
            // organizationArchivedExclusion.
            subscriptionFilterArchived = new Subscription
            {
                Name = "Subscription filter archived",
                Project = project1,
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                FundsExpirationDate = today.AddDays(-30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsFundsAccumulable = false,
                IsArchived = true
            };
            DbContext.Subscriptions.Add(subscriptionFilterArchived);

            var subscriptionTypeArchived = new SubscriptionType { Subscription = subscriptionFilterArchived, ProductGroup = productGroup, Amount = 10 };
            DbContext.SubscriptionTypes.Add(subscriptionTypeArchived);

            // 500 - 200 = 300, on the archived subscription. Must contribute zero everywhere, whether
            // or not the filter names it explicitly.
            var subscriptionTxArchived = new SubscriptionAddingFundTransaction
            {
                Organization = organizationSubscriptionFilter,
                SubscriptionType = subscriptionTypeArchived,
                ProductGroup = productGroup,
                ExpirationDate = today.AddMonths(1),
                Status = FundTransactionStatus.Actived,
                Amount = 500,
                AvailableFund = 200
            };
            DbContext.Transactions.Add(subscriptionTxArchived);

            // --- Fact 6: envelope precedence --------------------------------------------------

            subscriptionArchivedFutureExpiration = new Subscription
            {
                Name = "Subscription archived, funds expiration in the future",
                Project = project1,
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                FundsExpirationDate = today.AddDays(30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsFundsAccumulable = true,
                IsArchived = true
            };
            subscriptionArchivedPastExpiration = new Subscription
            {
                Name = "Subscription archived, funds expiration in the past",
                Project = project1,
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                FundsExpirationDate = today.AddDays(-30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsFundsAccumulable = true,
                IsArchived = true
            };
            DbContext.Subscriptions.AddRange(subscriptionArchivedFutureExpiration, subscriptionArchivedPastExpiration);

            // Wrongly counted (pre-existing defect, reproduced on purpose): the predicate is
            // `FundsExpirationDate >= today || (!IsFundsAccumulable && !IsArchived)`. A future
            // FundsExpirationDate makes the left side true, so the `||` short-circuits before the
            // archived check on the right side ever runs.
            var allowanceFuture = new BudgetAllowance
            {
                Organization = organizationEnvelopePrecedence,
                Subscription = subscriptionArchivedFutureExpiration,
                OriginalFund = 555,
                AvailableFund = 444
            };
            // Correctly excluded: same archived shape, but FundsExpirationDate already passed, so the
            // left side is false and the archived check on the right side applies.
            var allowancePast = new BudgetAllowance
            {
                Organization = organizationEnvelopePrecedence,
                Subscription = subscriptionArchivedPastExpiration,
                OriginalFund = 222,
                AvailableFund = 111
            };
            DbContext.BudgetAllowances.AddRange(allowanceFuture, allowancePast);

            // --- Fact 8: a second project, excluded ------------------------------------------

            var subscriptionOtherProject = new Subscription
            {
                Name = "Subscription other project",
                Project = project2,
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                FundsExpirationDate = today.AddDays(-30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsFundsAccumulable = false,
                IsArchived = false
            };
            DbContext.Subscriptions.Add(subscriptionOtherProject);

            var cardOtherProject = new Card { Project = project2, Status = CardStatus.Assigned, Funds = new List<Fund>(), Transactions = new List<Transaction>() };
            DbContext.Cards.Add(cardOtherProject);

            // A deliberately huge amount: if the handler ever aggregated across projects, this would
            // dominate project1's totals and make the leak obvious.
            var manualOtherProject = new ManuallyAddingFundTransaction
            {
                Organization = organizationOtherProject,
                Subscription = subscriptionOtherProject,
                ProductGroup = productGroupOtherProject,
                Card = cardOtherProject,
                ExpirationDate = today.AddMonths(1),
                Status = FundTransactionStatus.Actived,
                Amount = 99999,
                AvailableFund = 1
            };
            DbContext.Transactions.Add(manualOtherProject);

            DbContext.SaveChanges();

            handler = new GetOrganizationsStats(DbContext, Clock);
        }

        private Task<GetOrganizationsStats.Payload> RunAsync(IEnumerable<long> subscriptions = null)
        {
            return handler.Handle(new GetOrganizationsStats.Input
            {
                ProjectId = Id.New<Project>(project1.Id),
                Subscriptions = subscriptions
            }, CancellationToken.None);
        }

        private static GetOrganizationsStats.PayloadItem ItemFor(GetOrganizationsStats.Payload payload, Organization organization)
        {
            return payload.Items.Single(x => x.Organization.Id == organization.Id);
        }

        [Fact]
        public async Task BalanceOnCardsAndTotalAllocatedOnCardsExcludeTransactionsWithoutACard()
        {
            // Fact 1: both fields filter out CardId == null.
            var item = ItemFor(await RunAsync(), organizationCardAndBranches);

            // The CardId-null line (Amount 200 / AvailableFund 999) must not reach either field.
            item.BalanceOnCards.Should().Be(50); // 30 (active) + 20 (expired, no eft)
            item.TotalAllocatedOnCards.Should().Be(180); // 100 (active) + 80 (expired, no eft)
        }

        [Fact]
        public async Task CardSpendingAmountsIncludesTransactionsWithoutACard()
        {
            // Fact 2: unlike BalanceOnCards/TotalAllocatedOnCards, CardSpendingAmounts has no CardId
            // filter, so the card-id-null line (200 - 50 = 150) still contributes. This is the
            // asymmetry named as the easiest one to erase while rewriting.
            var item = ItemFor(await RunAsync(), organizationCardAndBranches);

            item.CardSpendingAmounts.Should().Be(280); // 70 (active) + 150 (expired, no card) + 60 (expired, no eft)
        }

        [Fact]
        public async Task CardSpendingAmountsComputesEachStatusBranchDifferently()
        {
            // Fact 3: active -> Amount - AvailableFund; expired with an ExpireFundTransaction ->
            // Amount - that transaction's Amount; expired without one -> falls back to Amount -
            // AvailableFund. The three contributions (70, 150, 60) are individually distinct, so a
            // wrong formula in any branch changes this total.
            var item = ItemFor(await RunAsync(), organizationCardAndBranches);

            item.CardSpendingAmounts.Should().Be(280);
        }

        [Fact]
        public async Task ArchivedSubscriptionLinesAreExcludedWithAndWithoutTheSubscriptionsFilter()
        {
            // Fact 4: the archived subscription's lines are excluded in both branches of the handler.
            var withoutFilter = ItemFor(await RunAsync(), organizationArchivedExclusion);
            withoutFilter.CardSpendingAmounts.Should().Be(31); // 25 (kept, active) + 6 (kept, expired)

            var withFilter = ItemFor(await RunAsync(new[] { subscriptionKept.Id, subscriptionDropped.Id }), organizationArchivedExclusion);
            // Even when the archived subscription is explicitly named in the filter, its lines stay excluded.
            withFilter.CardSpendingAmounts.Should().Be(31);
            // Fact 9, exercised under the filter this time: the archived subscription's expiration (444)
            // stays excluded from ExpiredAmounts even though it is explicitly named in the filter.
            withFilter.ExpiredAmounts.Should().Be(4);
        }

        [Fact]
        public async Task SubscriptionsFilterAppliesToSubscriptionAddingFundTransactionsByTypeSubscriptionId()
        {
            // Fact 5: the filter restricts subscription-adding-fund transactions via
            // SubscriptionType.SubscriptionId.
            var withoutFilter = ItemFor(await RunAsync(), organizationSubscriptionFilter);
            withoutFilter.CardSpendingAmounts.Should().Be(85); // 35 (subscriptionFilterOne) + 50 (subscriptionFilterTwo)

            var withFilter = ItemFor(await RunAsync(new[] { subscriptionFilterOne.Id }), organizationSubscriptionFilter);
            // Only subscriptionFilterOne was requested: subscriptionFilterTwo's line disappears even
            // though it is not archived.
            withFilter.CardSpendingAmounts.Should().Be(35);
        }

        [Fact]
        public async Task ArchivedSubscriptionLinesAreExcludedForSubscriptionAddingFundTransactionsToo()
        {
            // Fact 4, applied to SubscriptionAddingFundTransaction specifically: the archived-subscription
            // exclusion (":54"/":83") also holds for this transaction type, not just
            // ManuallyAddingFundTransaction (already pinned by ArchivedSubscriptionLinesAreExcludedWithAndWithoutTheSubscriptionsFilter).
            var withoutFilter = ItemFor(await RunAsync(), organizationSubscriptionFilter);
            withoutFilter.CardSpendingAmounts.Should().Be(85); // unaffected: the archived line (300) stays excluded

            var withFilter = ItemFor(await RunAsync(new[] { subscriptionFilterOne.Id, subscriptionFilterArchived.Id }), organizationSubscriptionFilter);
            // Even when the archived subscription is explicitly named in the filter, its line stays excluded.
            withFilter.CardSpendingAmounts.Should().Be(35);
        }

        [Fact]
        public async Task ArchivedEnvelopeWithFutureFundsExpirationIsStillCounted()
        {
            // Fact 6, pre-existing defect reproduced on purpose (not fixed here): the predicate is
            // `FundsExpirationDate >= today || (!IsFundsAccumulable && !IsArchived)`, so a future
            // FundsExpirationDate short-circuits the `||` before the archived check ever runs.
            var item = ItemFor(await RunAsync(), organizationEnvelopePrecedence);

            item.TotalActiveSubscriptionsEnvelopes.Should().Be(555); // only the future-expiring archived envelope
            item.RemainingPerEnvelope.Should().Be(444);
        }

        [Fact]
        public async Task BudgetAllowancesFilterExcludesArchivedSubscriptionsEvenWhenNamedInTheFilter()
        {
            // Fact 4, applied to BudgetAllowances in the filtered branch (":67"): unlike the
            // unfiltered branch's own defect at ":109"/":115" that still counts the future-expiring
            // archived envelope (see ArchivedEnvelopeWithFutureFundsExpirationIsStillCounted), the
            // filtered branch's `&& !x.Subscription.IsArchived` term removes both archived allowances
            // from the collection before that predicate ever runs, even though both are explicitly
            // named in the filter.
            var withFilter = ItemFor(
                await RunAsync(new[] { subscriptionArchivedFutureExpiration.Id, subscriptionArchivedPastExpiration.Id }),
                organizationEnvelopePrecedence);

            withFilter.TotalActiveSubscriptionsEnvelopes.Should().Be(0);
            withFilter.RemainingPerEnvelope.Should().Be(0);
        }

        [Fact]
        public async Task OrganizationWithoutTransactionsStillAppearsZeroed()
        {
            // Fact 7: an organization with no transaction still appears, every monetary field at zero.
            var payload = await RunAsync();
            payload.Items.Should().Contain(x => x.Organization.Id == organizationWithoutTransactions.Id);

            var item = ItemFor(payload, organizationWithoutTransactions);
            item.TotalActiveSubscriptionsEnvelopes.Should().Be(0);
            item.RemainingPerEnvelope.Should().Be(0);
            item.BalanceOnCards.Should().Be(0);
            item.TotalAllocatedOnCards.Should().Be(0);
            item.CardSpendingAmounts.Should().Be(0);
            item.ExpiredAmounts.Should().Be(0);
        }

        [Fact]
        public async Task TransactionsFromAnotherProjectDoNotContribute()
        {
            // Fact 8: project2's organization does not appear, and its huge amount does not leak
            // into project1's totals.
            var payload = await RunAsync();

            payload.Items.Should().NotContain(x => x.Organization.Id == organizationOtherProject.Id);

            var item = ItemFor(payload, organizationCardAndBranches);
            item.BalanceOnCards.Should().Be(50);
            item.TotalAllocatedOnCards.Should().Be(180);
        }

        [Fact]
        public async Task ExpiredAmountsOnlySumsNonArchivedSubscriptionExpirations()
        {
            // Fact 9: ExpiredAmounts sums only the expiration transactions of non-archived subscriptions.
            var item = ItemFor(await RunAsync(), organizationArchivedExclusion);

            // Only the kept subscription's expiration (4) counts; the dropped (archived) subscription's
            // expiration (444) is excluded.
            item.ExpiredAmounts.Should().Be(4);
        }
    }
}
