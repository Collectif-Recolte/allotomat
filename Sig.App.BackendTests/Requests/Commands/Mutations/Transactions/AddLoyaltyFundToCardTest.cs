using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Transactions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Transactions
{
    public class AddLoyaltyFundToCardTest : TestBase
    {
        private readonly AddLoyaltyFundToCard handler;

        private readonly Project project;
        private readonly Card card;
        private readonly Organization organization;
        private readonly Beneficiary beneficiary;
        
        public AddLoyaltyFundToCardTest()
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
                ProgramCardId = 1
            };
            
            beneficiary = new Beneficiary()
            {
                Organization = organization,
                Card = card
            };

            project.Cards = new List<Card> { card };

            var productGroupLoyalty = new ProductGroup()
            {
                Name = ProductGroupType.LOYALTY,
                Color = ProductGroupColor.Color_0,
                OrderOfAppearance = -1,
                Project = project
            };
            DbContext.ProductGroups.Add(productGroupLoyalty);

            DbContext.Cards.Add(card);
            DbContext.Organizations.Add(organization);
            DbContext.Projects.Add(project);
            DbContext.Beneficiaries.Add(beneficiary);

            DbContext.SaveChanges();

            handler = new AddLoyaltyFundToCard(NullLogger<AddLoyaltyFundToCard>.Instance, DbContext, Clock, HttpContextAccessor);
        }

        [Fact]
        public async Task AddLoyaltyFundToCard()
        {
            var input = new AddLoyaltyFundToCard.Input()
            {
                ProjectId = project.GetIdentifier(),
                CardId = card.ProgramCardId,
                Amount = 10
            };

            await handler.Handle(input, CancellationToken.None);

            card.LoyaltyFund().Should().Be(10);

            var transaction = await DbContext.Transactions.OfType<LoyaltyAddingFundTransaction>().FirstAsync();
            transaction.Amount.Should().Be(10);

            var transactionLog =
                await DbContext.TransactionLogs.FirstAsync(
                    x => x.TransactionUniqueId == transaction.TransactionUniqueId);
            transactionLog.TotalAmount.Should().Be(transaction.Amount);
        }

        [Fact]
        public async Task ThrowsIfCardNotFoundExceptiond()
        {
            var input = new AddLoyaltyFundToCard.Input()
            {
                ProjectId = project.GetIdentifier(),
                CardId = 123456,
                Amount = 10
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddLoyaltyFundToCard.CardNotFoundException>();
        }
    }
}
