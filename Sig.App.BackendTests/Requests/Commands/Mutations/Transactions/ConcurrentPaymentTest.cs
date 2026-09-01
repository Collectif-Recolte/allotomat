using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Transactions;
using Sig.App.Backend.Requests.Queries.Cards;
using Sig.App.Backend.Services.Mailer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Transactions
{
    // ------------------------------------------------------------------------------------------
    // CRCL-2669 - Two purchases on the same card that read the same balance must both be applied,
    // and the two counters (Fund.Amount, and the sum of AvailableFund of the active deposits) must
    // still agree afterwards.
    //
    // This is the "assertions the right way round" version of the reproduction written for the
    // remediation dossier (!5464): that file was green *because* the defect existed and asked to be
    // inverted, not deleted, the day concurrency got protected. This is that day.
    //
    // Production, August 1st 2026: while the deposit job held its locks, cashiers retried. On card
    // 8797 two identical 32 $ payments consumed the same deposit slices, because both had read the
    // same snapshot; their writes collapsed into one and the card lost a debit on one counter and
    // two on the other.
    //
    // The interleaving is forced by hand: both contexts read before either writes. No parallelism,
    // no scheduler. Deterministic, or it proves nothing.
    // ------------------------------------------------------------------------------------------
    public class ConcurrentPaymentTest : TestBase
    {
        private readonly Project project;
        private readonly Market market;
        private readonly ProductGroup productGroup;
        private readonly Card card;
        private readonly Fund fund;

        public ConcurrentPaymentTest()
        {
            project = new Project { Name = "Project 1" };
            market = new Market { Name = "Market 1" };

            productGroup = new ProductGroup
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };
            DbContext.ProductGroups.Add(productGroup);

            card = new Card
            {
                Funds = new List<Fund>(),
                Status = CardStatus.Assigned,
                Project = project,
                CardNumber = "1111-2222-3333-4444"
            };

            // 40 $ on the product group, always supposed to equal the two active deposits below.
            fund = new Fund { Amount = 40, Card = card, ProductGroup = productGroup };
            card.Funds.Add(fund);

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            var firstOfMonth = new DateTime(today.Year, today.Month, 1);

            // Two subscriptions only so that the two deposits are distinct rows; their content is
            // irrelevant to the scenario.
            var subscription1 = NewSubscription("Subscription 1", firstOfMonth);
            var subscription2 = NewSubscription("Subscription 2", firstOfMonth);

            card.Transactions = new List<Transaction>
            {
                NewDeposit("deposit-1", subscription1, firstOfMonth, today),
                NewDeposit("deposit-2", subscription2, firstOfMonth, today)
            };
            project.Subscriptions = new List<Subscription> { subscription1, subscription2 };
            project.Cards = new List<Card> { card };

            DbContext.Markets.Add(market);
            DbContext.Cards.Add(card);
            DbContext.Subscriptions.AddRange(subscription1, subscription2);
            DbContext.Projects.Add(project);
            DbContext.SaveChanges();

            DbContext.ProjectMarkets.Add(new ProjectMarket { MarketId = market.Id, ProjectId = project.Id });
            DbContext.SaveChanges();
        }

        private Subscription NewSubscription(string name, DateTime firstOfMonth) => new()
        {
            Name = name,
            Project = project,
            Types = new List<SubscriptionType>(),
            MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
            StartDate = firstOfMonth,
            EndDate = firstOfMonth.AddMonths(1)
        };

        private ManuallyAddingFundTransaction NewDeposit(string uniqueId, Subscription subscription, DateTime firstOfMonth, DateTime today) => new()
        {
            TransactionUniqueId = uniqueId,
            Amount = 20,
            AvailableFund = 20,
            Card = card,
            Subscription = subscription,
            ProductGroup = productGroup,
            Status = FundTransactionStatus.Actived,
            ExpirationDate = firstOfMonth.AddMonths(1),
            CreatedAtUtc = today
        };

        private CreateTransaction BuildHandler(AppDbContext context)
        {
            var mediator = new Mock<IMediator>();
            mediator
                .Setup(x => x.Send(It.IsAny<VerifyCardCanBeUsedInMarket.Input>(), It.IsAny<CancellationToken>()))
                .Returns<VerifyCardCanBeUsedInMarket.Input, CancellationToken>(
                    (request, token) => new VerifyCardCanBeUsedInMarket(context).Handle(request, token));

            return new CreateTransaction(NullLogger<CreateTransaction>.Instance, context, mediator.Object,
                Mock.Of<IMailer>(), Clock, HttpContextAccessor);
        }

        private CreateTransaction.Input BuildInput(decimal amount) => new()
        {
            MarketId = market.GetIdentifier(),
            CardId = card.GetIdentifier(),
            Transactions = new List<CreateTransaction.TransactionInput>
            {
                new() { Amount = amount, ProductGroupId = productGroup.GetIdentifier() }
            }
        };

        private async Task<decimal> DepositAvailableAsync(AppDbContext verify, string uniqueId) =>
            await verify.Transactions.OfType<ManuallyAddingFundTransaction>().AsNoTracking()
                .Where(x => x.TransactionUniqueId == uniqueId).Select(x => x.AvailableFund).SingleAsync();

        [Fact]
        public async Task TwoPurchasesReadingTheSameBalance_AreBothApplied_AndTheCountersStillAgree()
        {
            var contextA = CreateDbContext();
            var contextB = CreateDbContext();

            // Both reads first. Each context tracks its own copy of the card, funds and deposits
            // before either purchase writes; identity resolution then keeps that snapshot for the
            // handler's own queries, even after the other context has written.
            await contextA.Cards.Include(x => x.Funds).Include(x => x.Transactions).FirstOrDefaultAsync(x => x.Id == card.Id);
            await contextB.Cards.Include(x => x.Funds).Include(x => x.Transactions).FirstOrDefaultAsync(x => x.Id == card.Id);

            // Purchase A: 25 $, empties deposit-1 (20) and takes 5 from deposit-2.
            // Purchase B: 10 $, planned on its stale snapshot against deposit-1, which A has emptied.
            await BuildHandler(contextA).Handle(BuildInput(25), CancellationToken.None);
            await BuildHandler(contextB).Handle(BuildInput(10), CancellationToken.None);

            var verify = CreateDbContext();

            var payments = await verify.Transactions.OfType<PaymentTransaction>().CountAsync(x => x.CardId == card.Id);
            var finalFundAmount = await verify.Funds.AsNoTracking().Where(x => x.Id == fund.Id).Select(x => x.Amount).SingleAsync();
            var deposit1 = await DepositAvailableAsync(verify, "deposit-1");
            var deposit2 = await DepositAvailableAsync(verify, "deposit-2");

            // Neither purchase was refused: the card had 40 $, they add up to 35 $.
            payments.Should().Be(2);

            // 40 - 25 - 10. Pre-fix: 30, purchase A's write entirely overwritten by B's.
            finalFundAmount.Should().Be(5);

            // B could not have been allocated to deposit-1, which no longer had anything: it is
            // re-planned on fresh data and lands on deposit-2. Pre-fix: deposit-2 ends at 10, the
            // 5 $ that A took from it silently lost.
            deposit1.Should().Be(0);
            deposit2.Should().Be(5);

            // The invariant this whole family of defects breaks: the two counters agree.
            finalFundAmount.Should().Be(deposit1 + deposit2);
        }

        [Fact]
        public async Task SecondPurchaseThatTheRemainingFundsCannotCover_IsRefused_AndNothingIsOverdrawn()
        {
            var contextA = CreateDbContext();
            var contextB = CreateDbContext();

            await contextA.Cards.Include(x => x.Funds).Include(x => x.Transactions).FirstOrDefaultAsync(x => x.Id == card.Id);
            await contextB.Cards.Include(x => x.Funds).Include(x => x.Transactions).FirstOrDefaultAsync(x => x.Id == card.Id);

            // A spends 30 of the 40. B, on the same 40 $ snapshot, wants 15: only 10 are left.
            await BuildHandler(contextA).Handle(BuildInput(30), CancellationToken.None);
            Func<Task> second = () => BuildHandler(contextB).Handle(BuildInput(15), CancellationToken.None);

            await second.Should().ThrowAsync<CreateTransaction.NotEnoughtFundException>();

            var verify = CreateDbContext();
            var finalFundAmount = await verify.Funds.AsNoTracking().Where(x => x.Id == fund.Id).Select(x => x.Amount).SingleAsync();
            var payments = await verify.Transactions.OfType<PaymentTransaction>().CountAsync(x => x.CardId == card.Id);

            payments.Should().Be(1);
            finalFundAmount.Should().Be(10);
            finalFundAmount.Should().Be(await DepositAvailableAsync(verify, "deposit-1") + await DepositAvailableAsync(verify, "deposit-2"));
        }
    }
}
