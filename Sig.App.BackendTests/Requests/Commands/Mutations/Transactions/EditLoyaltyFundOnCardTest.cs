using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Transactions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Transactions
{
    public class EditLoyaltyFundOnCardTest : TestBase
    {
        private readonly EditLoyaltyFundOnCard handler;

        private readonly Project project;
        private readonly Card card;
        private readonly Organization organization;
        private readonly Beneficiary beneficiary;

        public EditLoyaltyFundOnCardTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };

            organization = new Organization()
            {
                Name = "Organization 1",
                Project = project
            };

            card = new Card()
            {
                Project = project,
                ProgramCardId = 1,
                Funds = new List<Fund>()
            };

            beneficiary = new Beneficiary()
            {
                Organization = organization
            };

            project.Cards = new List<Card> { card };

            var productGroupLoyalty = new ProductGroup()
            {
                Name = ProductGroupType.LOYALTY,
                Color = ProductGroupColor.Color_0,
                OrderOfAppearance = -1,
                Project = project
            };

            var fund = new Fund()
            {
                ProductGroup = productGroupLoyalty,
                Amount = 10,
                Card = card
            };
            card.Funds.Add(fund);

            DbContext.Funds.Add(fund);
            DbContext.ProductGroups.Add(productGroupLoyalty);
            DbContext.Cards.Add(card);
            DbContext.Organizations.Add(organization);
            DbContext.Projects.Add(project);
            DbContext.Beneficiaries.Add(beneficiary);

            DbContext.SaveChanges();

            handler = new EditLoyaltyFundOnCard(NullLogger<EditLoyaltyFundOnCard>.Instance, DbContext, Clock, HttpContextAccessor);
        }

        [Fact]
        public async Task EditLoyaltyFundOnCard()
        {
            var input = new EditLoyaltyFundOnCard.Input()
            {
                CardId = card.GetIdentifier(),
                Amount = 20
            };

            await handler.Handle(input, CancellationToken.None);

            card.LoyaltyFund().Should().Be(20);

            var transaction = await DbContext.Transactions.OfType<LoyaltyEditFundTransaction>().FirstAsync();
            transaction.Amount.Should().Be(10);

            var transactionLog =
                await DbContext.TransactionLogs.FirstAsync(
                    x => x.TransactionUniqueId == transaction.TransactionUniqueId);
            transactionLog.TotalAmount.Should().Be(transaction.Amount);
        }

        [Fact]
        public async Task RemoveLoyaltyFundOnCard()
        {
            var input = new EditLoyaltyFundOnCard.Input()
            {
                CardId = card.GetIdentifier(),
                Amount = 0
            };

            await handler.Handle(input, CancellationToken.None);

            card.LoyaltyFund().Should().Be(0);
            card.Status.Should().Be(CardStatus.Unassigned);
            card.Funds.Count.Should().Be(0);

            var transaction = await DbContext.Transactions.OfType<LoyaltyEditFundTransaction>().FirstAsync();
            transaction.Amount.Should().Be(-10);

            var transactionLog =
                await DbContext.TransactionLogs.FirstAsync(
                    x => x.TransactionUniqueId == transaction.TransactionUniqueId);
            transactionLog.TotalAmount.Should().Be(transaction.Amount);
        }

        [Fact]
        public async Task RemoveLoyaltyFundOnCardWithBeneficiary()
        {
            card.Beneficiary = beneficiary;
            card.Status = CardStatus.Assigned;
            DbContext.SaveChanges();

            var input = new EditLoyaltyFundOnCard.Input()
            {
                CardId = card.GetIdentifier(),
                Amount = 0
            };

            await handler.Handle(input, CancellationToken.None);

            card.LoyaltyFund().Should().Be(0);
            card.Status.Should().Be(CardStatus.Assigned);
            card.Funds.Count.Should().Be(0);

            var transaction = await DbContext.Transactions.OfType<LoyaltyEditFundTransaction>().FirstAsync();
            transaction.Amount.Should().Be(-10);

            var transactionLog =
                await DbContext.TransactionLogs.FirstAsync(
                    x => x.TransactionUniqueId == transaction.TransactionUniqueId);
            transactionLog.TotalAmount.Should().Be(transaction.Amount);
        }

        [Fact]
        public async Task ThrowsIfLoyaltyFundCantBeNegativeException()
        {
            var input = new EditLoyaltyFundOnCard.Input()
            {
                CardId = card.GetIdentifier(),
                Amount = -1
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditLoyaltyFundOnCard.LoyaltyFundCantBeNegativeException>();
        }

        [Fact]
        public async Task ThrowsIfCardIsNotGiftCardException()
        {
            var localCard = new Card()
            {
                Project = project,
                ProgramCardId = 2,
                Funds = new List<Fund>()
            };
            DbContext.Cards.Add(localCard);
            DbContext.SaveChanges();

            var input = new EditLoyaltyFundOnCard.Input()
            {
                CardId = localCard.GetIdentifier(),
                Amount = 10
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditLoyaltyFundOnCard.CardIsNotGiftCardException>();
        }

        [Fact]
        public async Task ThrowsIfCardNotFoundException()
        {
            var input = new EditLoyaltyFundOnCard.Input()
            {
                CardId = Id.New<Card>(123456),
                Amount = 10
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditLoyaltyFundOnCard.CardNotFoundException>();
        }
    }
}
