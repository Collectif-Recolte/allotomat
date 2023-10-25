using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Beneficiaries
{
    public class EditBeneficiaryTest : TestBase
    {
        private readonly EditBeneficiary handler;
        private readonly Beneficiary beneficiary;
        private readonly BeneficiaryType beneficiaryType;

        public EditBeneficiaryTest()
        {
            var project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            var organization = new Organization()
            {
                Project = project,
                Name = "Organization 1"
            };
            DbContext.Organizations.Add(organization);

            beneficiaryType = new BeneficiaryType()
            {
                Keys = "Type1",
                Name = "Type1",
                Project = project
            };
            DbContext.BeneficiaryTypes.Add(beneficiaryType);

            beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Address = "123, Example Street"
            };
            DbContext.Beneficiaries.Add(beneficiary);

            DbContext.SaveChanges();

            handler = new EditBeneficiary(NullLogger<EditBeneficiary>.Instance, DbContext);
        }

        [Fact]
        public async Task EditTheBeneficiary()
        {
            var input = new EditBeneficiary.Input()
            {
                Firstname = new Maybe<NonNull<string>>("John Test"),
                Lastname = new Maybe<NonNull<string>>("Doe Test"),
                Email = new Maybe<NonNull<string>>("john.doe-test@example.com"),
                Phone = new Maybe<NonNull<string>>("555-555-5678"),
                Address = new Maybe<NonNull<string>>("123, Test Street"),
                Notes = new Maybe<NonNull<string>>("Notes 1234"),
                PostalCode = new Maybe<NonNull<string>>("A0A 0A0"),
                Id1 = new Maybe<NonNull<string>>("ID1 1234"),
                Id2 = new Maybe<NonNull<string>>("ID2 1234"),
                BeneficiaryId = beneficiary.GetIdentifier(),
                BeneficiaryTypeId = beneficiaryType.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiary = await DbContext.Beneficiaries.FirstAsync();

            localBeneficiary.Firstname.Should().Be("John Test");
            localBeneficiary.Lastname.Should().Be("Doe Test");
            localBeneficiary.Email.Should().Be("john.doe-test@example.com");
            localBeneficiary.Phone.Should().Be("555-555-5678");
            localBeneficiary.Address.Should().Be("123, Test Street");
            localBeneficiary.Notes.Should().Be("Notes 1234");
            localBeneficiary.PostalCode.Should().Be("A0A 0A0");
            localBeneficiary.ID1.Should().Be("ID1 1234");
            localBeneficiary.ID2.Should().Be("ID2 1234");
            localBeneficiary.BeneficiaryTypeId.Should().Be(beneficiaryType.Id);
        }

        [Fact]
        public async Task EditOnlyFirstname()
        {
            var input = new EditBeneficiary.Input()
            {
                Firstname = new Maybe<NonNull<string>>("John Test"),
                BeneficiaryId = beneficiary.GetIdentifier(),
                BeneficiaryTypeId = beneficiaryType.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiary = await DbContext.Beneficiaries.FirstAsync();

            localBeneficiary.Firstname.Should().Be("John Test");
            localBeneficiary.Lastname.Should().Be("Doe");
            localBeneficiary.Email.Should().Be("john.doe@example.com");
            localBeneficiary.Phone.Should().Be("555-555-1234");
            localBeneficiary.Address.Should().Be("123, Example Street");
            localBeneficiary.BeneficiaryTypeId.Should().Be(beneficiaryType.Id);
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotFound()
        {
            var input = new EditBeneficiary.Input()
            {
                Firstname = new Maybe<NonNull<string>>("John Test"),
                Lastname = new Maybe<NonNull<string>>("Doe Test"),
                Email = new Maybe<NonNull<string>>("john.doe-test@example.com"),
                Phone = new Maybe<NonNull<string>>("555-555-5678"),
                Address = new Maybe<NonNull<string>>("123, Test Street"),
                BeneficiaryId = Id.New<Beneficiary>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditBeneficiary.BeneficiaryNotFoundException>();
        }
    }
}
