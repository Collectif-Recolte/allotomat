using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
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
    public class DeleteBeneficiaryTypeTest : TestBase
    {
        private readonly IRequestHandler<DeleteBeneficiaryType.Input> handler;
        private readonly BeneficiaryType beneficiaryType;

        public DeleteBeneficiaryTypeTest()
        {
            var project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            beneficiaryType = new BeneficiaryType()
            {
                Name = "Type 1",
                Keys = "A;B;C;D",
                Project = project
            };
            DbContext.BeneficiaryTypes.Add(beneficiaryType);

            DbContext.SaveChanges();

            handler = new DeleteBeneficiaryType(NullLogger<DeleteBeneficiaryType>.Instance, DbContext);
        }

        [Fact]
        public async Task DeleteTheBeneficiaryType()
        {
            var input = new DeleteBeneficiaryType.Input()
            {
                BeneficiaryTypeId = beneficiaryType.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var beneficiarycount = await DbContext.BeneficiaryTypes.CountAsync();
            beneficiarycount.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryTypeNotFound()
        {
            var input = new DeleteBeneficiaryType.Input()
            {
                BeneficiaryTypeId = Id.New<BeneficiaryType>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteBeneficiaryType.BeneficiaryTypeNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryTypeHaveBeneficiaries()
        {
            var organization = new Organization()
            {
                Name = "Organization 1",
                Project = beneficiaryType.Project
            };
            DbContext.Organizations.Add(organization);

            var beneficiary = new Beneficiary()
            {
                BeneficiaryType = beneficiaryType,
                Firstname = "John",
                Lastname = "Doe",
                Organization = organization
            };
            DbContext.Beneficiaries.Add(beneficiary);

            DbContext.SaveChanges();

            var input = new DeleteBeneficiaryType.Input()
            {
                BeneficiaryTypeId = beneficiaryType.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteBeneficiaryType.BeneficiaryTypeCantHaveBeneficiariesException>();
        }
    }
}
