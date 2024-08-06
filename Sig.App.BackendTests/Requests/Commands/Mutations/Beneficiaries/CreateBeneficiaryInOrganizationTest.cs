using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Beneficiaries
{
    public class CreateBeneficiaryInOrganizationTest : TestBase
    {
        private readonly CreateBeneficiaryInOrganization handler;
        private readonly Organization organization;
        private readonly BeneficiaryType beneficiaryType;

        public CreateBeneficiaryInOrganizationTest()
        {
            var project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            organization = new Organization()
            {
                Name = "Organization 1",
                Project = project
            };
            DbContext.Organizations.Add(organization);

            beneficiaryType = new BeneficiaryType()
            {
                Keys = "bliblou",
                Name = "Type 1",
                Project = project
            };
            DbContext.BeneficiaryTypes.Add(beneficiaryType);

            DbContext.SaveChanges();

            handler = new CreateBeneficiaryInOrganization(NullLogger<CreateBeneficiaryInOrganization>.Instance, DbContext);
        }

        [Fact]
        public async Task CreateTheBeneficiary()
        {
            var input = new CreateBeneficiaryInOrganization.Input()
            {
                Firstname = "John",
                Lastname = " Doe ",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Address = "123, Example Street",
                OrganizationId = organization.GetIdentifier(),
                BeneficiaryTypeId = beneficiaryType.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var beneficiary = await DbContext.Beneficiaries.FirstAsync();

            beneficiary.Firstname.Should().Be("John");
            beneficiary.Lastname.Should().Be("Doe");
            beneficiary.Email.Should().Be("john.doe@example.com");
            beneficiary.Phone.Should().Be("555-555-1234");
            beneficiary.Address.Should().Be("123, Example Street");
            beneficiary.Notes.Should().Be(null);
            beneficiary.BeneficiaryTypeId.Should().Be(beneficiaryType.Id);
            beneficiary.ID1.Length.Should().Be(36);
        }

        [Fact]
        public async Task CreateTheBeneficiaryWithoutCommunicationMeans()
        {
            var input = new CreateBeneficiaryInOrganization.Input()
            {
                Firstname = "John",
                Lastname = "Doe",
                OrganizationId = organization.GetIdentifier(),
                BeneficiaryTypeId = beneficiaryType.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var beneficiary = await DbContext.Beneficiaries.FirstAsync();

            beneficiary.Firstname.Should().Be("John");
            beneficiary.Lastname.Should().Be("Doe");
            beneficiary.Email.Should().Be(null);
            beneficiary.Phone.Should().Be(null);
            beneficiary.Address.Should().Be(null);
            beneficiary.BeneficiaryTypeId.Should().Be(beneficiaryType.Id);
        }

        [Fact]
        public async Task CreateTheBeneficiaryWithAllInformations()
        {
            var input = new CreateBeneficiaryInOrganization.Input()
            {
                Firstname = "John",
                Lastname = "Doe",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Address = "123, Example Street",
                Notes = "Notes 1234",
                PostalCode = "A0A 0A0",
                Id1 = "ID1 1234",
                Id2 = "ID2 1234",
                OrganizationId = organization.GetIdentifier(),
                BeneficiaryTypeId = beneficiaryType.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var beneficiary = await DbContext.Beneficiaries.FirstAsync();

            beneficiary.Firstname.Should().Be("John");
            beneficiary.Lastname.Should().Be("Doe");
            beneficiary.Email.Should().Be("john.doe@example.com");
            beneficiary.Phone.Should().Be("555-555-1234");
            beneficiary.Address.Should().Be("123, Example Street");
            beneficiary.Notes.Should().Be("Notes 1234");
            beneficiary.PostalCode.Should().Be("A0A 0A0");
            beneficiary.ID1.Should().Be("ID1 1234");
            beneficiary.ID2.Should().Be("ID2 1234");
            beneficiary.BeneficiaryTypeId.Should().Be(beneficiaryType.Id);
        }

        [Fact]
        public async Task ThrowsIfOrganizationNotFound()
        {
            var input = new CreateBeneficiaryInOrganization.Input()
            {
                Firstname = "John",
                Lastname = "Doe",
                OrganizationId = Id.New<Organization>(123456),
                BeneficiaryTypeId = beneficiaryType.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateBeneficiaryInOrganization.OrganizationNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryTypeNotFound()
        {
            var input = new CreateBeneficiaryInOrganization.Input()
            {
                Firstname = "John",
                Lastname = "Doe",
                OrganizationId = organization.GetIdentifier(),
                BeneficiaryTypeId = Id.New<BeneficiaryType>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateBeneficiaryInOrganization.BeneficiaryTypeNotFoundException>();
        }
    }
}
