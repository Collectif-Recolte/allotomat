using FluentAssertions;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.BackgroundJobs;
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
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Transactions;
using Sig.App.Backend.Requests.Queries.Cards;
using Sig.App.Backend.Services.Mailer;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.BackgroundJobs
{
    // ------------------------------------------------------------------------------------------
    // CRCL-2669 - The card whose displayed balance (Fund.Amount) exceeded what the register would
    // let it spend (the sum of AvailableFund of its active deposits) by exactly the payments made
    // while the monthly deposit job was running.
    //
    // What production did on August 1st, 2026, reconstructed from the transaction ids: the job
    // loaded every card with its Funds at 15:37, computed `fund.Amount += 36` in memory for 22k
    // cards, and wrote the result back as an absolute value in a single SaveChanges minutes later.
    // Two purchases of 32 $ on the same card landed in between. Their debit of Fund.Amount was
    // overwritten by the job's stale 96 (60 + 36); the deposits' AvailableFund, which the job never
    // touches, kept the debit. From then on the card showed 32 $ more than it could spend.
    //
    // The interleaving is forced by hand, on two DbContexts: the job's context tracks the card and
    // its Fund BEFORE the purchase commits on another context, exactly as the real job holds its
    // snapshot for the whole run. EF Core's identity resolution then makes the job's own query
    // return that tracked, stale Fund instead of refreshing it - no scheduler, no Task.WhenAll.
    //
    // The test is red on the pre-fix code (Fund ends at 96) and green once Fund.Amount is written
    // as a movement rebased on the persisted value (64) - see FundConcurrencyExtensions.
    // ------------------------------------------------------------------------------------------
    public class AddingFundToCardConcurrencyTest : TestBase
    {
        private const decimal InitialFund = 60m;
        private const decimal MonthlyDeposit = 36m;
        private const decimal Purchase = 32m;

        private readonly Project project;
        private readonly Market market;
        private readonly ProductGroup productGroup;
        private readonly Card card;
        private readonly Fund fund;

        public AddingFundToCardConcurrencyTest()
        {
            project = new Project { Name = "Project 1" };
            market = new Market { Name = "Market 1" };

            var organization = new Organization { Name = "Organization 1", Project = project };
            var beneficiaryType = new BeneficiaryType { Name = "Type 1", Project = project, Keys = "type1" };
            var beneficiary = new Beneficiary
            {
                Firstname = "John",
                Lastname = "Doe",
                Organization = organization,
                BeneficiaryType = beneficiaryType
            };

            productGroup = new ProductGroup
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };

            card = new Card
            {
                Status = CardStatus.Assigned,
                Project = project,
                Beneficiary = beneficiary,
                CardNumber = "1111-2222-3333-4444",
                Funds = new List<Fund>(),
                Transactions = new List<Transaction>()
            };

            fund = new Fund { Amount = InitialFund, ProductGroup = productGroup, Card = card };
            card.Funds.Add(fund);

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            var firstOfMonth = new DateTime(today.Year, today.Month, 1);

            var subscription = new Subscription
            {
                Name = "Subscription 1",
                Project = project,
                Types = new List<SubscriptionType>
                {
                    new() { BeneficiaryType = beneficiaryType, Amount = MonthlyDeposit, ProductGroup = productGroup }
                },
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                StartDate = firstOfMonth,
                EndDate = firstOfMonth.AddMonths(1),
                FundsExpirationDate = firstOfMonth.AddMonths(2)
            };

            // The active deposit the purchase will be allocated to. Its AvailableFund is the counter
            // the job never writes, and the one the register actually spends from.
            card.Transactions.Add(new ManuallyAddingFundTransaction
            {
                TransactionUniqueId = "initial-deposit",
                Amount = InitialFund,
                AvailableFund = InitialFund,
                Card = card,
                Beneficiary = beneficiary,
                Subscription = subscription,
                ProductGroup = productGroup,
                Status = FundTransactionStatus.Actived,
                ExpirationDate = firstOfMonth.AddMonths(2),
                CreatedAtUtc = today
            });

            organization.Beneficiaries = new List<Beneficiary> { beneficiary };
            beneficiary.Card = card;
            project.Subscriptions = new List<Subscription> { subscription };
            project.Organizations = new List<Organization> { organization };
            project.Cards = new List<Card> { card };

            DbContext.Markets.Add(market);
            DbContext.Cards.Add(card);
            DbContext.Beneficiaries.Add(beneficiary);
            DbContext.Organizations.Add(organization);
            DbContext.Subscriptions.Add(subscription);
            DbContext.Projects.Add(project);
            DbContext.SaveChanges();

            var subscriptionBeneficiary = new SubscriptionBeneficiary
            {
                Beneficiary = beneficiary,
                Subscription = subscription,
                BeneficiaryType = beneficiaryType
            };
            subscription.BudgetAllowances = new List<BudgetAllowance>
            {
                new()
                {
                    Beneficiaries = new List<SubscriptionBeneficiary> { subscriptionBeneficiary },
                    Organization = organization,
                    AvailableFund = 2500,
                    OriginalFund = 5000
                }
            };
            DbContext.ProjectMarkets.Add(new ProjectMarket { MarketId = market.Id, ProjectId = project.Id });
            DbContext.SaveChanges();
        }

        private CreateTransaction BuildPaymentHandler(AppDbContext context)
        {
            var mediator = new Mock<IMediator>();
            mediator
                .Setup(x => x.Send(It.IsAny<VerifyCardCanBeUsedInMarket.Input>(), It.IsAny<CancellationToken>()))
                .Returns<VerifyCardCanBeUsedInMarket.Input, CancellationToken>(
                    (request, token) => new VerifyCardCanBeUsedInMarket(context).Handle(request, token));

            return new CreateTransaction(NullLogger<CreateTransaction>.Instance, context, mediator.Object,
                Mock.Of<IMailer>(), Clock, HttpContextAccessor);
        }

        private async Task<(decimal FundAmount, decimal ActiveDepositsAvailable)> PersistedCountersAsync()
        {
            var verify = CreateDbContext();
            var fundAmount = await verify.Funds.AsNoTracking()
                .Where(x => x.Id == fund.Id).Select(x => x.Amount).SingleAsync();
            var available = await verify.Transactions.OfType<AddingFundTransaction>().AsNoTracking()
                .Where(x => x.CardId == card.Id && x.ProductGroupId == productGroup.Id && x.Status == FundTransactionStatus.Actived)
                .SumAsync(x => x.AvailableFund);
            return (fundAmount, available);
        }

        [Fact]
        public async Task PurchaseCommittedWhileTheJobHoldsItsSnapshot_IsNotErasedFromTheFundAmount()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 1, 8, 0));

            // The job reads first: every card and its Funds are tracked in its context before any
            // purchase of the day. This is the snapshot the real job carried from 15:37 to its
            // SaveChanges, minutes later.
            var jobContext = CreateDbContext();
            await jobContext.Cards.Include(x => x.Funds).FirstAsync(x => x.Id == card.Id);

            // Then the purchase lands, on its own context, and commits: Fund 60 -> 28, deposit 60 -> 28.
            var paymentContext = CreateDbContext();
            await BuildPaymentHandler(paymentContext).Handle(new CreateTransaction.Input
            {
                MarketId = market.GetIdentifier(),
                CardId = card.GetIdentifier(),
                Transactions = new List<CreateTransaction.TransactionInput>
                {
                    new() { Amount = Purchase, ProductGroupId = productGroup.GetIdentifier() }
                }
            }, CancellationToken.None);

            (await PersistedCountersAsync()).Should().Be((InitialFund - Purchase, InitialFund - Purchase));

            // Then the job writes, from its stale snapshot: 60 + 36 in memory.
            var job = new AddingFundToCard(jobContext, Clock, NullLogger<AddingFundToCard>.Instance);
            await job.Run("AddFundToCard", new[] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });

            var (fundAmount, activeDepositsAvailable) = await PersistedCountersAsync();

            // The deposit was delivered...
            activeDepositsAvailable.Should().Be(InitialFund - Purchase + MonthlyDeposit);

            // ...and the purchase survived it. Pre-fix this reads 96: the job's absolute write erased
            // the 32 $ debit, which is the production defect to the cent.
            fundAmount.Should().Be(InitialFund - Purchase + MonthlyDeposit);
            fundAmount.Should().Be(activeDepositsAvailable);
        }

        [Fact]
        public void Run_IsGuardedAgainstConcurrentExecution()
        {
            // On August 1st the job was triggered twice by hand, eleven seconds apart, after the
            // scheduled run had timed out. Nothing stopped the second execution: the only guard reads
            // the AddingFundToCardRuns table, which neither run had written yet. Hangfire's
            // distributed lock is what makes the second one wait for the first, and then return
            // through that same guard.
            var run = typeof(AddingFundToCard).GetMethod(nameof(AddingFundToCard.Run));

            run.Should().NotBeNull();
            run.GetCustomAttribute<DisableConcurrentExecutionAttribute>().Should().NotBeNull(
                "two concurrent runs of the deposit job each overwrite Fund.Amount with their own stale snapshot");
        }
    }
}
