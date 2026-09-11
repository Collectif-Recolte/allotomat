using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Extensions
{
    // ------------------------------------------------------------------------------------------
    // CRCL-2669 - The contract of the joint on the two card counters, Fund.Amount and
    // AddingFundTransaction.AvailableFund. BudgetAllowanceConcurrencyExtensionsTest covers the
    // envelope rules; AddingFundToCardConcurrencyTest and ConcurrentPaymentTest prove the two
    // production scenarios end to end. These tests pin the rules themselves.
    //
    // The last two tests matter most: the rebase that runs BEFORE SaveChanges is enough to make every
    // other test here green even without a concurrency token. Only a write that bypasses the joint
    // proves the token is armed - and the token is what closes the window between re-read and write.
    // ------------------------------------------------------------------------------------------
    public class FundConcurrencyExtensionsTest : TestBase
    {
        private readonly Fund fund;
        private readonly ManuallyAddingFundTransaction deposit;

        public FundConcurrencyExtensionsTest()
        {
            var project = new Project { Name = "Project 1" };
            var productGroup = new ProductGroup
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };
            var card = new Card { Status = CardStatus.Assigned, Project = project, Funds = new List<Fund>(), Transactions = new List<Transaction>() };

            fund = new Fund { Amount = 100m, Card = card, ProductGroup = productGroup };
            card.Funds.Add(fund);

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            var subscription = new Subscription
            {
                Name = "Subscription 1",
                Project = project,
                Types = new List<SubscriptionType>(),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1)
            };

            deposit = new ManuallyAddingFundTransaction
            {
                TransactionUniqueId = "deposit",
                Amount = 100m,
                AvailableFund = 100m,
                Card = card,
                Subscription = subscription,
                ProductGroup = productGroup,
                Status = FundTransactionStatus.Actived,
                ExpirationDate = today.AddMonths(1),
                CreatedAtUtc = today
            };
            card.Transactions.Add(deposit);

            DbContext.Projects.Add(project);
            DbContext.ProductGroups.Add(productGroup);
            DbContext.Subscriptions.Add(subscription);
            DbContext.Cards.Add(card);
            DbContext.SaveChanges();
        }

        private async Task<(AppDbContext Context, Fund Fund)> ReadFundAsync()
        {
            var context = CreateDbContext();
            return (context, await context.Funds.FirstAsync(x => x.Id == fund.Id));
        }

        private async Task<(AppDbContext Context, ManuallyAddingFundTransaction Deposit)> ReadDepositAsync()
        {
            var context = CreateDbContext();
            return (context, await context.Transactions.OfType<ManuallyAddingFundTransaction>().FirstAsync(x => x.Id == deposit.Id));
        }

        private async Task<decimal> PersistedFundAsync() =>
            await CreateDbContext().Funds.AsNoTracking().Where(x => x.Id == fund.Id).Select(x => x.Amount).SingleAsync();

        private async Task<decimal> PersistedDepositAsync() =>
            await CreateDbContext().Transactions.OfType<ManuallyAddingFundTransaction>().AsNoTracking()
                .Where(x => x.Id == deposit.Id).Select(x => x.AvailableFund).SingleAsync();

        [Fact]
        public async Task CreditAndDebitReadingTheSameFundAmount_BothLand()
        {
            var (contextA, fundA) = await ReadFundAsync();
            var (contextB, fundB) = await ReadFundAsync();

            // The deposit job's write, and a purchase, both planned on 100.
            fundA.Amount += 36m;
            await contextA.SaveChangesWithFundRetryAsync(CancellationToken.None);

            fundB.Amount -= 32m;
            await contextB.SaveChangesWithFundRetryAsync(CancellationToken.None);

            // 100 + 36 - 32. Pre-fix the second write lands at 68 and the deposit is gone.
            (await PersistedFundAsync()).Should().Be(104m);
        }

        [Fact]
        public async Task DebitWhoseFundsWereSpentConcurrently_IsRefusedRatherThanOverdrawn()
        {
            var (contextA, fundA) = await ReadFundAsync();
            var (contextB, fundB) = await ReadFundAsync();

            fundA.Amount -= 100m;
            await contextA.SaveChangesWithFundRetryAsync(CancellationToken.None);

            fundB.Amount -= 40m;
            Func<Task> secondDebit = () => contextB.SaveChangesWithFundRetryAsync(CancellationToken.None);

            await secondDebit.Should().ThrowAsync<CardFundInsufficientException>();
            (await PersistedFundAsync()).Should().Be(0m);
        }

        [Fact]
        public async Task CreditIntoAFundAlreadyBelowZero_IsNotRefused()
        {
            // Negative card funds exist in production (fix/neutraliser-lignes-negatives counts them).
            // A refund must still land there: refusing it would keep the participant's money.
            var (setup, fundToDrain) = await ReadFundAsync();
            fundToDrain.Amount = -20m;
            await setup.SaveChangesAsync();

            var (context, fundToCredit) = await ReadFundAsync();
            fundToCredit.Amount += 15m;
            await context.SaveChangesWithFundRetryAsync(CancellationToken.None);

            (await PersistedFundAsync()).Should().Be(-5m);
        }

        [Fact]
        public async Task TwoDebitsOfTheSameDeposit_BothLand()
        {
            var (contextA, depositA) = await ReadDepositAsync();
            var (contextB, depositB) = await ReadDepositAsync();

            depositA.AvailableFund -= 25m;
            await contextA.SaveChangesWithFundRetryAsync(CancellationToken.None);

            depositB.AvailableFund -= 10m;
            await contextB.SaveChangesWithFundRetryAsync(CancellationToken.None);

            (await PersistedDepositAsync()).Should().Be(65m);
        }

        [Fact]
        public async Task DepositDebitTheRemainingAvailableFundCannotCover_IsRefused()
        {
            var (contextA, depositA) = await ReadDepositAsync();
            var (contextB, depositB) = await ReadDepositAsync();

            depositA.AvailableFund = 0m;
            await contextA.SaveChangesWithFundRetryAsync(CancellationToken.None);

            depositB.AvailableFund -= 10m;
            Func<Task> secondDebit = () => contextB.SaveChangesWithFundRetryAsync(CancellationToken.None);

            await secondDebit.Should().ThrowAsync<CardFundInsufficientException>();
            (await PersistedDepositAsync()).Should().Be(0m);
        }

        [Fact]
        public async Task ChangingAnotherPropertyOfADepositSomeoneElseDebited_KeepsTheirDebit()
        {
            // The token guards every UPDATE of the row, not only the ones that touch AvailableFund.
            // Marking a deposit expired from a stale snapshot must neither fail nor resurrect the
            // amount a concurrent purchase just consumed: a zero movement rebases to the persisted value.
            var (contextA, depositA) = await ReadDepositAsync();
            var (contextB, depositB) = await ReadDepositAsync();

            depositA.AvailableFund -= 30m;
            await contextA.SaveChangesWithFundRetryAsync(CancellationToken.None);

            depositB.Status = FundTransactionStatus.Expired;
            await contextB.SaveChangesWithFundRetryAsync(CancellationToken.None);

            var verify = CreateDbContext();
            var persisted = await verify.Transactions.OfType<ManuallyAddingFundTransaction>().AsNoTracking()
                .Where(x => x.Id == deposit.Id).Select(x => new { x.AvailableFund, x.Status }).SingleAsync();

            persisted.AvailableFund.Should().Be(70m);
            persisted.Status.Should().Be(FundTransactionStatus.Expired);
        }

        [Fact]
        public async Task StaleFundWriteThatBypassesTheJoint_IsRejectedByTheToken()
        {
            // Without this the whole file stays green with the IsConcurrencyToken line deleted from
            // AppDbContext: the rebase before SaveChanges hides its absence. This is the mutation that
            // must go red.
            var (contextA, fundA) = await ReadFundAsync();
            var (contextB, fundB) = await ReadFundAsync();

            fundA.Amount -= 32m;
            await contextA.SaveChangesAsync();

            fundB.Amount += 36m;
            Func<Task> staleWrite = () => contextB.SaveChangesAsync();

            await staleWrite.Should().ThrowAsync<DbUpdateConcurrencyException>();
            (await PersistedFundAsync()).Should().Be(68m);
        }

        [Fact]
        public async Task StaleDepositWriteThatBypassesTheJoint_IsRejectedByTheToken()
        {
            var (contextA, depositA) = await ReadDepositAsync();
            var (contextB, depositB) = await ReadDepositAsync();

            depositA.AvailableFund -= 32m;
            await contextA.SaveChangesAsync();

            depositB.AvailableFund -= 10m;
            Func<Task> staleWrite = () => contextB.SaveChangesAsync();

            await staleWrite.Should().ThrowAsync<DbUpdateConcurrencyException>();
            (await PersistedDepositAsync()).Should().Be(68m);
        }
    }
}
