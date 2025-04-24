using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Sig.App.Backend.Extensions;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Beneficiaries
{
    public class UnsubscribeBeneficiaryFromTransactionReceiptTest : TestBase
    {
        private readonly UnsubscribeBeneficiaryFromTransactionReceipt handler;
        private readonly Beneficiary beneficiary;

        public UnsubscribeBeneficiaryFromTransactionReceiptTest()
        {
            var project = new Project()
            {
                Name = "Project 1"
            };

            var organization = new Organization()
            {
                Name = "Organization1",
                Project = project
            };

            var beneficiaryType = new BeneficiaryType()
            {
                Name = "Type 1",
                Keys = "type1",
                Project = project
            };

            DbContext.Projects.Add(project);
            DbContext.Organizations.Add(organization);
            DbContext.BeneficiaryTypes.Add(beneficiaryType);
            DbContext.SaveChanges();

            beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Address = "123, Example Street",
                Organization = organization,
                BeneficiaryTypeId = beneficiaryType.Id
            };

            DbContext.Beneficiaries.Add(beneficiary);
            DbContext.SaveChanges();

            handler = new UnsubscribeBeneficiaryFromTransactionReceipt(NullLogger<UnsubscribeBeneficiaryFromTransactionReceipt>.Instance, DbContext);
        }

        [Fact]
        public async Task UnsubscribeOneBeneficiaryFromTransactionReceipt()
        {
            var input = new UnsubscribeBeneficiaryFromTransactionReceipt.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiary = DbContext.Beneficiaries.FirstOrDefault();
            localBeneficiary.IsUnsubscribeToReceipt.Should().BeTrue();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryDontHaveEmail()
        {
            beneficiary.Email = "";
            await DbContext.SaveChangesAsync();

            var input = new UnsubscribeBeneficiaryFromTransactionReceipt.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<UnsubscribeBeneficiaryFromTransactionReceipt.BeneficiaryDontHaveEmailException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotFound()
        {
            var input = new UnsubscribeBeneficiaryFromTransactionReceipt.Input()
            {
                BeneficiaryId = Id.New<Beneficiary>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<UnsubscribeBeneficiaryFromTransactionReceipt.BeneficiaryNotFoundException>();
        }
    }
}
