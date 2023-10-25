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
    public class AssignCardToBeneficiaryTest : TestBase
    {
        private readonly AssignCardToBeneficiary handler;
        private readonly Beneficiary beneficiary;
        private readonly Card card;

        public AssignCardToBeneficiaryTest()
        {
            var project = new Project()
            {
                Name = "Project 1"
            };

            card = new Card()
            {
                Status = CardStatus.Unassigned,
                Project = project,
                ProgramCardId = 1
            };

            DbContext.Cards.Add(card);

            var organization = new Organization()
            {
                Project = project
            };

            beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Address = "123, example street",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Organization = organization
            };
            DbContext.Beneficiaries.Add(beneficiary);

            DbContext.SaveChanges();

            handler = new AssignCardToBeneficiary(NullLogger<AssignCardToBeneficiary>.Instance, DbContext);
        }

        [Fact]
        public async Task CreateAssignCardToBeneficiary()
        {
            var input = new AssignCardToBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                CardId = card.ProgramCardId
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiary = DbContext.Beneficiaries.First();

            localBeneficiary.CardId.Should().Be(card.Id);
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotFound()
        {
            var input = new AssignCardToBeneficiary.Input()
            {
                BeneficiaryId = Id.New<Beneficiary>(123456),
                CardId = card.ProgramCardId
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignCardToBeneficiary.BeneficiaryNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfCardNotFound()
        {
            var input = new AssignCardToBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                CardId = 987654321
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignCardToBeneficiary.CardNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfCardAlreadyAssignException()
        {
            card.Status = CardStatus.Assigned;
            DbContext.SaveChanges();

            var input = new AssignCardToBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                CardId = card.ProgramCardId
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignCardToBeneficiary.CardAlreadyAssignException>();
        }

        [Fact]
        public async Task ThrowsIfCardLostException()
        {
            card.Status = CardStatus.Lost;
            DbContext.SaveChanges();

            var input = new AssignCardToBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                CardId = card.ProgramCardId
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignCardToBeneficiary.CardLostException>();
        }

        [Fact]
        public async Task ThrowsIfCardDeactivatedException()
        {
            card.Status = CardStatus.Deactivated;
            DbContext.SaveChanges();

            var input = new AssignCardToBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                CardId = card.ProgramCardId
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignCardToBeneficiary.CardDeactivatedException>();
        }

        [Fact]
        public async Task ThrowsIfCardAlreadyGiftCardException()
        {
            card.Status = CardStatus.GiftCard;
            DbContext.SaveChanges();

            var input = new AssignCardToBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                CardId = card.ProgramCardId
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignCardToBeneficiary.CardAlreadyGiftCardException>();
        }
    }
}
