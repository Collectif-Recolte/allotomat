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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Cards
{
    public class AssignUnassignedCardToBeneficiaryTest : TestBase
    {
        private readonly AssignUnassignedCardToBeneficiary handler;
        private readonly Beneficiary beneficiary;
        private readonly Card card;

        public AssignUnassignedCardToBeneficiaryTest()
        {
            var project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            var organization = new Organization()
            {
                Name = "Organization 1",
                Project = project
            };
            DbContext.Organizations.Add(organization);

            card = new Card()
            {
                Status = CardStatus.Unassigned,
                Project = project
            };

            DbContext.Cards.Add(card);

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

            handler = new AssignUnassignedCardToBeneficiary(NullLogger<AssignUnassignedCardToBeneficiary>.Instance, DbContext);
        }

        [Fact]
        public async Task AssignRandomUnassignedCardToBeneficiary()
        {
            var input = new AssignUnassignedCardToBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiary = DbContext.Beneficiaries.First();
            localBeneficiary.CardId.Should().Be(card.Id);
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotFound()
        {
            var input = new AssignUnassignedCardToBeneficiary.Input()
            {
                BeneficiaryId = Id.New<Beneficiary>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignUnassignedCardToBeneficiary.BeneficiaryNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfNoUnassignedCardAvailable()
        {
            card.Status = CardStatus.Assigned;
            DbContext.SaveChanges();

            var input = new AssignUnassignedCardToBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignUnassignedCardToBeneficiary.NoUnassignedCardAvailableException>();
        }
    }
}
