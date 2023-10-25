using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Cards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Cards
{
    public class TransfertCardTest : TestBase
    {
        private readonly TransfertCard handler;
        private readonly Card originalCard;
        private readonly Card newCard;
        private readonly ProductGroup productGroup;

        public TransfertCardTest()
        {
            var project = new Project()
            {
                Name = "Project 1"
            };

            DbContext.Projects.Add(project);

            var beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Address = "123, example street",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Organization = new Organization()
            };
            DbContext.Beneficiaries.Add(beneficiary);

            productGroup = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };
            DbContext.ProductGroups.Add(productGroup);

            var loyaltyProductgroup = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_0,
                Name = ProductGroupType.LOYALTY,
                OrderOfAppearance = -1
            };
            DbContext.ProductGroups.Add(loyaltyProductgroup);

            originalCard = new Card()
            {
                Project = project,
                ProgramCardId = 1
            };

            originalCard.Funds = new List<Fund>
            {
                new Fund()
                {
                    Amount = 400,
                    Card = originalCard,
                    ProductGroup = productGroup
                },
                new Fund()
                {
                    Amount = 200,
                    Card = originalCard,
                    ProductGroup = loyaltyProductgroup
                }
            };

            originalCard.Status = CardStatus.Assigned;
            originalCard.Beneficiary = beneficiary;
            originalCard.Transactions = new List<Transaction>()
            {
                new LoyaltyAddingFundTransaction()
                {
                    Card = originalCard,
                    Amount = 200m,
                    AvailableFund = 200m,
                    CreatedAtUtc = DateTime.Now,
                    ProductGroup = loyaltyProductgroup
                },
                new ManuallyAddingFundTransaction()
                {
                    Card = originalCard,
                    Amount = 400,
                    AvailableFund = 400,
                    Beneficiary = beneficiary,
                    CreatedAtUtc = DateTime.Now,
                    ProductGroup = productGroup
                }
            };

            newCard = new Card()
            {
                Project = project,
                ProgramCardId = 2
            };

            DbContext.Cards.Add(originalCard);
            DbContext.Cards.Add(newCard);

            DbContext.SaveChanges();

            handler = new TransfertCard(NullLogger<TransfertCard>.Instance, DbContext, Clock, HttpContextAccessor);
        }

        [Fact]
        public async Task TransfertCard()
        {
            var input = new TransfertCard.Input()
            {
                OriginalCardId = originalCard.GetIdentifier(),
                NewCardId = newCard.ProgramCardId
            };

            await handler.Handle(input, CancellationToken.None);

            var localOriginalCard = await DbContext.Cards.Include(x => x.Funds).Where(x => x.Id == originalCard.Id).FirstAsync();
            var localNewCard = await DbContext.Cards.Include(x => x.Funds).Include(x => x.Transactions).Include(x => x.Beneficiary).Where(x => x.Id == newCard.Id).FirstAsync();

            localOriginalCard.Funds.Count().Should().Be(0);
            localOriginalCard.Status.Should().Be(CardStatus.Lost);
            localOriginalCard.Transactions.Count.Should().Be(0);
            localOriginalCard.Beneficiary.Should().Be(null);

            localNewCard.Funds.First().Amount.Should().Be(400);
            localNewCard.Funds.First(x => x.ProductGroup.Name == ProductGroupType.LOYALTY).Amount.Should().Be(200);
            localNewCard.Status.Should().Be(CardStatus.Assigned);
            localNewCard.Transactions.Count.Should().Be(2);
            localNewCard.Beneficiary.Firstname.Should().Be("John");

            var transactionLogCreated = await DbContext.TransactionLogs
                .Where(x => x.Discriminator == TransactionLogDiscriminator.TransferFundTransactionLog).ToListAsync();
            transactionLogCreated.Count.Should().Be(2);
        }

        [Fact]
        public async Task ThrowsIfOriginalCardNotFoundException()
        {
            var input = new TransfertCard.Input()
            {
                NewCardId = 1,
                OriginalCardId = Id.New<Card>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<TransfertCard.OriginalCardNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfNewCardNotFoundException()
        {
            var input = new TransfertCard.Input()
            {
                NewCardId = 123456,
                OriginalCardId = originalCard.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<TransfertCard.NewCardNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfNewCardAlreadyAssignException()
        {
            var localBeneficiary = new Beneficiary()
            {
                Firstname = "John2",
                Lastname = "Doe",
                Address = "123, example street",
                Email = "john2.doe@example.com",
                Phone = "555-555-1234"
            };
            DbContext.Beneficiaries.Add(localBeneficiary);
            newCard.Beneficiary = localBeneficiary;
            newCard.Status = Backend.DbModel.Enums.CardStatus.Assigned;
            DbContext.SaveChanges();

            var input = new TransfertCard.Input()
            {
                NewCardId = newCard.ProgramCardId,
                OriginalCardId = originalCard.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<TransfertCard.NewCardAlreadyAssignException>();
        }
    }
}
