using DocumentFormat.OpenXml.Office2010.Excel;
using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Beneficiaries
{
    public class ImportOffPlatformBeneficiariesListInOrganizationTest : TestBase
    {
        private readonly ImportOffPlatformBeneficiariesListInOrganization handler;
        private readonly Organization organization;
        private readonly ProductGroup productGroup1;
        private readonly Project project;

        public ImportOffPlatformBeneficiariesListInOrganizationTest()
        {
            project = new Project()
            {
                Name = "Project 1",
                AdministrationSubscriptionsOffPlatform = true
            };
            DbContext.Projects.Add(project);

            organization = new Organization()
            {
                Name = "Organization 1",
                Project = project
            };
            DbContext.Organizations.Add(organization);

            productGroup1 = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "ProductGroup1"
            };

            var productGroup2 = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_2,
                Name = "ProductGroup2"
            };
            DbContext.ProductGroups.Add(productGroup1);
            DbContext.ProductGroups.Add(productGroup2);

            DbContext.SaveChanges();

            handler = new ImportOffPlatformBeneficiariesListInOrganization(NullLogger<ImportOffPlatformBeneficiariesListInOrganization>.Instance, DbContext, Clock, HttpContextAccessor);
        }

        [Fact]
        public async Task ImportOneBeneficiary()
        {
            var input = new ImportOffPlatformBeneficiariesListInOrganization.Input()
            {
                Items = new List<ImportOffPlatformBeneficiariesListInOrganization.OffPlatformBeneficiaryItem>()
                {
                    new ImportOffPlatformBeneficiariesListInOrganization.OffPlatformBeneficiaryItem()
                    {
                        Id1 = "1",
                        Firstname = "John",
                        Lastname = "Doe",
                        Email = "john.doe@example.com",
                        Phone = "555-555-1234",
                        Address = "123, Example Street",
                        Notes = "Lorem ipsum",
                        StartDate = new LocalDate(2022, 1, 1),
                        MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                        Funds = new List<ImportOffPlatformBeneficiariesListInOrganization.FundType>() {
                            new ImportOffPlatformBeneficiariesListInOrganization.FundType()
                            {
                                Amount = 10,
                                ProductGroupName = "ProductGroup1"
                            },
                            new ImportOffPlatformBeneficiariesListInOrganization.FundType()
                            {
                                Amount = 20,
                                ProductGroupName = "ProductGroup2"
                            }
                        }
                    }
                }.ToArray(),
                OrganizationId = organization.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var beneficiary = await DbContext.Beneficiaries.FirstAsync() as OffPlatformBeneficiary;

            beneficiary.ID1.Should().Be("1");
            beneficiary.Firstname.Should().Be("John");
            beneficiary.Lastname.Should().Be("Doe");
            beneficiary.Email.Should().Be("john.doe@example.com");
            beneficiary.Phone.Should().Be("555-555-1234");
            beneficiary.Address.Should().Be("123, Example Street");
            beneficiary.Notes.Should().Be("Lorem ipsum");
            beneficiary.MonthlyPaymentMoment.Should().Be(SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth);
            beneficiary.PaymentFunds.Count().Should().Be(2);
            beneficiary.PaymentFunds.First().Amount.Should().Be(10);
            beneficiary.PaymentFunds.First().ProductGroupId.Should().Be(productGroup1.Id);
            beneficiary.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task UpdateBeneficiaryOnImport()
        {
            DbContext.Beneficiaries.Add(new OffPlatformBeneficiary()
            {
                ID1 = "1",
                Firstname = "John",
                Lastname = "Doe",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Address = "123, Example Street",
                OrganizationId = organization.Id,
                SortOrder = 0,
                IsActive = false,
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth
            });

            DbContext.Beneficiaries.Add(new OffPlatformBeneficiary()
            {
                ID1 = "4",
                Firstname = "Jane",
                Lastname = "Doe",
                Email = "hane.doe@example.com",
                Phone = "555-555-1234",
                Address = "123, Example Street",
                OrganizationId = organization.Id,
                SortOrder = 1,
                IsActive = true,
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth
            });

            DbContext.SaveChanges();

            var input = new ImportOffPlatformBeneficiariesListInOrganization.Input()
            {
                Items = new System.Collections.Generic.List<ImportOffPlatformBeneficiariesListInOrganization.OffPlatformBeneficiaryItem>()
                {
                    new ImportOffPlatformBeneficiariesListInOrganization.OffPlatformBeneficiaryItem()
                    {
                        Id1 = "1",
                        Firstname = "Jane",
                        Lastname = "Does",
                        Email = "jane.does@example.com",
                        Phone = "555-555-1235",
                        Address = "124, Example Street",
                        Notes = "Lorem ipsum",
                        MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                        StartDate = new LocalDate(2022, 1, 1),
                        Funds = new List<ImportOffPlatformBeneficiariesListInOrganization.FundType>() {
                            new ImportOffPlatformBeneficiariesListInOrganization.FundType()
                            {
                                Amount = 10,
                                ProductGroupName = "ProductGroup1"
                            },
                            new ImportOffPlatformBeneficiariesListInOrganization.FundType()
                            {
                                Amount = 20,
                                ProductGroupName = "ProductGroup2"
                            }
                        }
                    }
                }.ToArray(),
                OrganizationId = organization.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var beneficiary = await DbContext.Beneficiaries.Where(x => x.ID1 == "1").FirstAsync() as OffPlatformBeneficiary;

            beneficiary.ID1.Should().Be("1");
            beneficiary.Firstname.Should().Be("Jane");
            beneficiary.Lastname.Should().Be("Does");
            beneficiary.Email.Should().Be("jane.does@example.com");
            beneficiary.Phone.Should().Be("555-555-1235");
            beneficiary.Address.Should().Be("124, Example Street");
            beneficiary.Notes.Should().Be("Lorem ipsum");
            beneficiary.MonthlyPaymentMoment.Should().Be(SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth);
            beneficiary.PaymentFunds.Count().Should().Be(2);
            beneficiary.PaymentFunds.First().Amount.Should().Be(10);
            beneficiary.PaymentFunds.First().ProductGroupId.Should().Be(productGroup1.Id);
            beneficiary.IsActive.Should().BeTrue();

            var beneficiary2 = await DbContext.Beneficiaries.Where(x => x.ID1 == "4").FirstAsync() as OffPlatformBeneficiary;
            beneficiary2.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task UpdateBeneficiarySortOnImport()
        {
            DbContext.Beneficiaries.Add(new OffPlatformBeneficiary()
            {
                ID1 = "1",
                Firstname = "John",
                Lastname = "Doe",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Address = "123, Example Street",
                OrganizationId = organization.Id,
                SortOrder = 0
            });
            DbContext.SaveChanges();

            var input = new ImportOffPlatformBeneficiariesListInOrganization.Input()
            {
                Items = new List<ImportOffPlatformBeneficiariesListInOrganization.OffPlatformBeneficiaryItem>()
                {
                    new ImportOffPlatformBeneficiariesListInOrganization.OffPlatformBeneficiaryItem()
                    {
                        Id1 = "2",
                        Firstname = "Jane",
                        Lastname = "Does",
                        Email = "jane.does@example.com",
                        Phone = "555-555-1235",
                        Address = "124, Example Street",
                        MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                        StartDate = new LocalDate(2022, 1, 1),
                        EndDate = new LocalDate(2022, 3, 30),
                        Funds = new List<ImportOffPlatformBeneficiariesListInOrganization.FundType>() {
                            new ImportOffPlatformBeneficiariesListInOrganization.FundType()
                            {
                                Amount = 10,
                                ProductGroupName = "ProductGroup1"
                            },
                            new ImportOffPlatformBeneficiariesListInOrganization.FundType()
                            {
                                Amount = 20,
                                ProductGroupName = "ProductGroup2"
                            }
                        }
                    }
                }.ToArray(),
                OrganizationId = organization.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var beneficiary = await DbContext.Beneficiaries.FirstAsync() as OffPlatformBeneficiary;
            var lastBeneficiary = await DbContext.Beneficiaries.LastAsync() as OffPlatformBeneficiary;

            lastBeneficiary.ID1.Should().Be("2");
            lastBeneficiary.Firstname.Should().Be("Jane");
            lastBeneficiary.Lastname.Should().Be("Does");
            lastBeneficiary.Email.Should().Be("jane.does@example.com");
            lastBeneficiary.Phone.Should().Be("555-555-1235");
            lastBeneficiary.Address.Should().Be("124, Example Street");
            lastBeneficiary.SortOrder.Should().Be(0);

            beneficiary.SortOrder.Should().Be(1);
        }

        [Fact]
        public async Task ThrowsIfOrganizationNotFound()
        {
            var input = new ImportOffPlatformBeneficiariesListInOrganization.Input()
            {
                Items = new System.Collections.Generic.List<ImportOffPlatformBeneficiariesListInOrganization.OffPlatformBeneficiaryItem>()
                {
                    new ImportOffPlatformBeneficiariesListInOrganization.OffPlatformBeneficiaryItem()
                    {
                        Firstname = "John",
                        Lastname = "Doe",
                        Email = "john.doe@example.com",
                        Phone = "555-555-1234",
                        Address = "123, Example Street"
                    }
                }.ToArray(),
                OrganizationId = GraphQL.Conventions.Id.New<Organization>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ImportOffPlatformBeneficiariesListInOrganization.OrganizationNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfProjectDontAdministrateSubscriptionOffPlatformException()
        {
            project.AdministrationSubscriptionsOffPlatform = false;
            await DbContext.SaveChangesAsync();

            var input = new ImportOffPlatformBeneficiariesListInOrganization.Input()
            {
                Items = new List<ImportOffPlatformBeneficiariesListInOrganization.OffPlatformBeneficiaryItem>()
                {
                    new ImportOffPlatformBeneficiariesListInOrganization.OffPlatformBeneficiaryItem()
                    {
                        Firstname = "John",
                        Lastname = "Doe",
                        Email = "john.doe@example.com",
                        Phone = "555-555-1234",
                        Address = "123, Example Street"
                    }
                }.ToArray(),
                OrganizationId = organization.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ImportOffPlatformBeneficiariesListInOrganization.ProjectDontAdministrateSubscriptionOffPlatformException>();
        }
    }
}
