using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Beneficiaries
{
    public class ImportBeneficiariesListInOrganizationTest : TestBase
    {
        private readonly ImportBeneficiariesListInOrganization handler;
        private readonly Organization organization;
        private readonly BeneficiaryType beneficiaryType;

        public ImportBeneficiariesListInOrganizationTest()
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
                Keys = "type1",
                Name = "Type 1",
                Project = project
            };
            DbContext.BeneficiaryTypes.Add(beneficiaryType);

            DbContext.SaveChanges();

            handler = new ImportBeneficiariesListInOrganization(NullLogger<ImportBeneficiariesListInOrganization>.Instance, DbContext);
        }

        [Fact]
        public async Task ImportOneBeneficiary()
        {
            var input = new ImportBeneficiariesListInOrganization.Input()
            {
                Items = new System.Collections.Generic.List<ImportBeneficiariesListInOrganization.BeneficiaryItem>()
                {
                    new ImportBeneficiariesListInOrganization.BeneficiaryItem()
                    {
                        Id1 = "1",
                        Firstname = "John",
                        Lastname = "Doe",
                        Email = "john.doe@example.com",
                        Phone = "555-555-1234",
                        Address = "123, Example Street",
                        Notes = "Lorem ipsum",
                        Key = "type1"
                    }
                },
                OrganizationId = organization.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var beneficiary = await DbContext.Beneficiaries.FirstAsync();

            beneficiary.ID1.Should().Be("1");
            beneficiary.Firstname.Should().Be("John");
            beneficiary.Lastname.Should().Be("Doe");
            beneficiary.Email.Should().Be("john.doe@example.com");
            beneficiary.Phone.Should().Be("555-555-1234");
            beneficiary.Address.Should().Be("123, Example Street");
            beneficiary.BeneficiaryTypeId.Should().Be(beneficiaryType.Id);
        }

        [Fact]
        public async Task ImportOneHundredBeneficiary()
        {
            var i = 0;
            var input = new ImportBeneficiariesListInOrganization.Input()
            {
                Items = Enumerable.Repeat(new ImportBeneficiariesListInOrganization.BeneficiaryItem()
                {
                    Id1 = (i++).ToString(),
                    Firstname = "John",
                    Lastname = "Doe",
                    Email = "john.doe@example.com",
                    Phone = "555-555-1234",
                    Address = "123, Example Street",
                    Key = "type1"
                }, 100).ToList(),
                OrganizationId = organization.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var beneficiary = await DbContext.Beneficiaries.FirstAsync();

            beneficiary.ID1.Should().Be("0");
            beneficiary.Firstname.Should().Be("John");
            beneficiary.Lastname.Should().Be("Doe");
            beneficiary.Email.Should().Be("john.doe@example.com");
            beneficiary.Phone.Should().Be("555-555-1234");
            beneficiary.Address.Should().Be("123, Example Street");
            beneficiary.BeneficiaryTypeId.Should().Be(beneficiaryType.Id);

            var beneficiaries = await DbContext.Beneficiaries.CountAsync();

            var lastBeneficiary = await DbContext.Beneficiaries.LastAsync();

            lastBeneficiary.SortOrder.Should().Be(99);

            beneficiaries.Should().Be(100);
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryTypeNotFound()
        {
            var input = new ImportBeneficiariesListInOrganization.Input()
            {
                Items = new System.Collections.Generic.List<ImportBeneficiariesListInOrganization.BeneficiaryItem>()
                {
                    new ImportBeneficiariesListInOrganization.BeneficiaryItem()
                    {
                        Id1 = "1",
                        Firstname = "John",
                        Lastname = "Doe",
                        Email = "john.doe@example.com",
                        Phone = "555-555-1234",
                        Address = "123, Example Street",
                        Key = "type2"
                    }
                },
                OrganizationId = organization.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ImportBeneficiariesListInOrganization.BeneficiaryTypeNotFoundException>();
        }

        [Fact]
        public async Task UpdateBeneficiaryOnImport()
        {
            DbContext.Beneficiaries.Add(new Beneficiary()
            {
                ID1 = "1",
                Firstname = "John",
                Lastname = "Doe",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Address = "123, Example Street",
                BeneficiaryTypeId = beneficiaryType.Id,
                OrganizationId = organization.Id,
                SortOrder = 0
            });
            DbContext.SaveChanges();

            var input = new ImportBeneficiariesListInOrganization.Input()
            {
                Items = new System.Collections.Generic.List<ImportBeneficiariesListInOrganization.BeneficiaryItem>()
                {
                    new ImportBeneficiariesListInOrganization.BeneficiaryItem()
                    {
                        Id1 = "1",
                        Firstname = "Jane",
                        Lastname = "Does",
                        Email = "jane.does@example.com",
                        Phone = "555-555-1235",
                        Address = "124, Example Street",
                        Key = "type1"
                    }
                },
                OrganizationId = organization.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var beneficiary = await DbContext.Beneficiaries.FirstAsync();

            var lastBeneficiaryType = await DbContext.BeneficiaryTypes.LastAsync();

            beneficiary.ID1.Should().Be("1");
            beneficiary.Firstname.Should().Be("Jane");
            beneficiary.Lastname.Should().Be("Does");
            beneficiary.Email.Should().Be("jane.does@example.com");
            beneficiary.Phone.Should().Be("555-555-1235");
            beneficiary.Address.Should().Be("124, Example Street");
            beneficiary.BeneficiaryTypeId.Should().Be(lastBeneficiaryType.Id);
        }

        [Fact]
        public async Task UpdateBeneficiarySortOnImport()
        {
            DbContext.Beneficiaries.Add(new Beneficiary()
            {
                ID1 = "1",
                Firstname = "John",
                Lastname = "Doe",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Address = "123, Example Street",
                BeneficiaryTypeId = beneficiaryType.Id,
                OrganizationId = organization.Id,
                SortOrder = 0
            });
            DbContext.SaveChanges();

            var input = new ImportBeneficiariesListInOrganization.Input()
            {
                Items = new System.Collections.Generic.List<ImportBeneficiariesListInOrganization.BeneficiaryItem>()
                {
                    new ImportBeneficiariesListInOrganization.BeneficiaryItem()
                    {
                        Id1 = "2",
                        Firstname = "Jane",
                        Lastname = "Does",
                        Email = "jane.does@example.com",
                        Phone = "555-555-1235",
                        Address = "124, Example Street",
                        Key = "type1"
                    }
                },
                OrganizationId = organization.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var beneficiary = await DbContext.Beneficiaries.FirstAsync();
            var lastBeneficiary = await DbContext.Beneficiaries.LastAsync();

            var lastBeneficiaryType = await DbContext.BeneficiaryTypes.LastAsync();

            lastBeneficiary.ID1.Should().Be("2");
            lastBeneficiary.Firstname.Should().Be("Jane");
            lastBeneficiary.Lastname.Should().Be("Does");
            lastBeneficiary.Email.Should().Be("jane.does@example.com");
            lastBeneficiary.Phone.Should().Be("555-555-1235");
            lastBeneficiary.Address.Should().Be("124, Example Street");
            lastBeneficiary.BeneficiaryTypeId.Should().Be(lastBeneficiaryType.Id);
            lastBeneficiary.SortOrder.Should().Be(0);

            beneficiary.SortOrder.Should().Be(1);
        }

        [Fact]
        public async Task ThrowsIfOrganizationNotFound()
        {
            var input = new ImportBeneficiariesListInOrganization.Input()
            {
                Items = new System.Collections.Generic.List<ImportBeneficiariesListInOrganization.BeneficiaryItem>()
                {
                    new ImportBeneficiariesListInOrganization.BeneficiaryItem()
                    {
                        Firstname = "John",
                        Lastname = "Doe",
                        Email = "john.doe@example.com",
                        Phone = "555-555-1234",
                        Address = "123, Example Street"
                    }
                },
                OrganizationId = Id.New<Organization>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ImportBeneficiariesListInOrganization.OrganizationNotFoundException>();
        }
    }
}
