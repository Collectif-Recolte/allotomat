using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Cards;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Cards
{
    public class UnlockCardTest : TestBase
    {
        private readonly UnlockCard handler;
        private readonly Card card;

        public UnlockCardTest()
        {
            var project = new Project()
            {
                Name = "Project 1"
            };

            card = new Card()
            {
                Status = CardStatus.Lost,
                Project = project,
                ProgramCardId = 1,
                IsDisabled = false
            };

            DbContext.Cards.Add(card);

            var organization = new Organization()
            {
                Project = project
            };

            var beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Address = "123, example street",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Organization = organization,
                Card = card
            };
            DbContext.Beneficiaries.Add(beneficiary);

            DbContext.SaveChanges();

            handler = new UnlockCard(NullLogger<UnlockCard>.Instance, DbContext);
        }

        [Fact]
        public async Task UnlockCard()
        {
            var input = new UnlockCard.Input()
            {
                CardId = card.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localCard = DbContext.Cards.First();

            localCard.Status.Should().Be(CardStatus.Unassigned);
        }

        [Fact]
        public async Task ThrowsIfCardNotFound()
        {
            var input = new UnlockCard.Input()
            {
                CardId = Id.New<Card>(123456),
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<UnlockCard.CardNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfCardNotLost()
        {
            card.Status = CardStatus.Unassigned;

            DbContext.SaveChanges();

            var input = new UnlockCard.Input()
            {
                CardId = card.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<UnlockCard.CardNotLostException>();
        }
    }
}
