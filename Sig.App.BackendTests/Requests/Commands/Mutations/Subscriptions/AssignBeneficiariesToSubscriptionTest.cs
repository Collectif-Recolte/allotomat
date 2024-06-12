using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Subscriptions
{
    public class AssignBeneficiariesToSubscriptionTest : TestBase
    {
        private readonly AssignBeneficiariesToSubscription handler;

        private readonly Project project;
        
        private readonly Organization organization;

        private readonly Subscription subscription1;
        private readonly Subscription subscription2;

        private readonly Subscription subscription3;

        private readonly Beneficiary beneficiary1;
        private readonly Beneficiary beneficiary2;
        private readonly Beneficiary beneficiary3;
        private readonly Beneficiary beneficiary4;
        private readonly Beneficiary beneficiary5;
        private readonly Beneficiary beneficiary6;
        private readonly Beneficiary beneficiary7;
        private readonly Beneficiary beneficiary8;

        private readonly BeneficiaryType beneficiaryType1;
        private readonly BeneficiaryType beneficiaryType2;

        private readonly BudgetAllowance budgetAllowance1;
        private readonly BudgetAllowance budgetAllowance2;
        private readonly BudgetAllowance budgetAllowance3;

        public AssignBeneficiariesToSubscriptionTest()
        {
            project = new Project()
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

            beneficiaryType1 = new BeneficiaryType()
            {
                Project = project,
                Keys = "Beneficiary type 1",
                Name = "Beneficiary type 1"
            };
            DbContext.BeneficiaryTypes.Add(beneficiaryType1);

            beneficiary1 = new Beneficiary()
            {
                SortOrder = 0,
                Organization = organization,
                Firstname = "John1",
                Lastname = "Doe1",
                Address = "123, example street",
                Email = "john1.doe1@example.com",
                Phone = "555-555-1234",
                BeneficiaryType = beneficiaryType1
            };
            DbContext.Beneficiaries.Add(beneficiary1);
            beneficiary2 = new Beneficiary()
            {
                SortOrder = 2,
                Organization = organization,
                Firstname = "John2",
                Lastname = "Doe2",
                Address = "123, example street",
                Email = "john2.doe2@example.com",
                Phone = "555-555-1234",
                BeneficiaryType = beneficiaryType1
            };
            DbContext.Beneficiaries.Add(beneficiary2);
            beneficiary3 = new Beneficiary()
            {
                SortOrder = 4,
                Organization = organization,
                Firstname = "John3",
                Lastname = "Doe3",
                Address = "123, example street",
                Email = "john3.doe3@example.com",
                Phone = "555-555-1234",
                BeneficiaryType = beneficiaryType1
            };
            DbContext.Beneficiaries.Add(beneficiary3);
            beneficiary4 = new Beneficiary()
            {
                SortOrder = 6,
                Organization = organization,
                Firstname = "John4",
                Lastname = "Doe4",
                Address = "123, example street",
                Email = "john4.doe4@example.com",
                Phone = "555-555-1234",
                BeneficiaryType = beneficiaryType1
            };
            DbContext.Beneficiaries.Add(beneficiary4);

            beneficiaryType2 = new BeneficiaryType()
            {
                Project = project,
                Keys = "Beneficiary type 2",
                Name = "Beneficiary type 2"
            };
            DbContext.BeneficiaryTypes.Add(beneficiaryType2);

            beneficiary5 = new Beneficiary()
            {
                SortOrder = 1,
                Organization = organization,
                Firstname = "John5",
                Lastname = "Doe5",
                Address = "123, example street",
                Email = "john5.doe5@example.com",
                Phone = "555-555-1234",
                BeneficiaryType = beneficiaryType2
            };
            DbContext.Beneficiaries.Add(beneficiary5);
            beneficiary6 = new Beneficiary()
            {
                SortOrder = 3,
                Organization = organization,
                Firstname = "John6",
                Lastname = "Doe6",
                Address = "123, example street",
                Email = "john6.doe6@example.com",
                Phone = "555-555-1234",
                BeneficiaryType = beneficiaryType2
            };
            DbContext.Beneficiaries.Add(beneficiary6);
            beneficiary7 = new Beneficiary()
            {
                SortOrder = 5,
                Organization = organization,
                Firstname = "John7",
                Lastname = "Doe7",
                Address = "123, example street",
                Email = "john7.doe7@example.com",
                Phone = "555-555-1234",
                BeneficiaryType = beneficiaryType2
            };
            DbContext.Beneficiaries.Add(beneficiary7);
            beneficiary8 = new Beneficiary()
            {
                SortOrder = 7,
                Organization = organization,
                Firstname = "John8",
                Lastname = "Doe8",
                Address = "123, example street",
                Email = "john8.doe8@example.com",
                Phone = "555-555-1234",
                BeneficiaryType = beneficiaryType2
            };
            DbContext.Beneficiaries.Add(beneficiary8);

            var productGroup1 = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };
            DbContext.ProductGroups.Add(productGroup1);

            var productGroup2 = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_2,
                Name = "Product group 2",
                OrderOfAppearance = 2
            };
            DbContext.ProductGroups.Add(productGroup2);

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            subscription1 = new Subscription()
            {
                Name = "Subscription 1",
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 50,
                        BeneficiaryType = beneficiaryType1,
                        ProductGroup = productGroup1
                    },
                    new SubscriptionType()
                    {
                        Amount = 100,
                        BeneficiaryType = beneficiaryType2,
                        ProductGroup = productGroup1
                    },
                    new SubscriptionType()
                    {
                        Amount = 10,
                        BeneficiaryType = beneficiaryType2,
                        ProductGroup = productGroup2
                    }
                },
                Project = project
            };
            DbContext.Subscriptions.Add(subscription1);

            subscription2 = new Subscription()
            {
                Name = "Subscription 2",
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 25,
                        BeneficiaryType = beneficiaryType1
                    },
                    new SubscriptionType()
                    {
                        Amount = 50,
                        BeneficiaryType = beneficiaryType2
                    }
                },
                Project = project
            };
            DbContext.Subscriptions.Add(subscription2);

            subscription3 = new Subscription()
            {
                Name = "Subscription 3",
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(4),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 25,
                        BeneficiaryType = beneficiaryType1
                    },
                    new SubscriptionType()
                    {
                        Amount = 50,
                        BeneficiaryType = beneficiaryType2
                    }
                },
                Project = project,
                IsSubscriptionPaymentBasedCardUsage = true,
                MaxNumberOfPayments = 4
            };
            DbContext.Subscriptions.Add(subscription3);

            budgetAllowance1 = new BudgetAllowance()
            {
                AvailableFund = 700,
                Organization = organization,
                Subscription = subscription1,
                OriginalFund = 700
            };
            DbContext.BudgetAllowances.Add(budgetAllowance1);

            budgetAllowance2 = new BudgetAllowance()
            {
                AvailableFund = 700,
                Organization = organization,
                Subscription = subscription2,
                OriginalFund = 700
            };
            DbContext.BudgetAllowances.Add(budgetAllowance2);

            budgetAllowance3 = new BudgetAllowance()
            {
                AvailableFund = 700,
                Organization = organization,
                Subscription = subscription3,
                OriginalFund = 700
            };
            DbContext.BudgetAllowances.Add(budgetAllowance3);

            DbContext.SaveChanges();

            handler = new AssignBeneficiariesToSubscription(NullLogger<AssignBeneficiariesToSubscription>.Instance, Clock, DbContext);

            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 4, 0, 0));
        }

        [Fact]
        public async Task AssignSubscriptionToAllBeneficiaries()
        {
            var input = new AssignBeneficiariesToSubscription.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = subscription1.GetIdentifier(),
                Beneficiaries = [beneficiary1.GetIdentifier(), beneficiary2.GetIdentifier(), beneficiary3.GetIdentifier(), beneficiary4.GetIdentifier(), beneficiary5.GetIdentifier(), beneficiary6.GetIdentifier(), beneficiary7.GetIdentifier(), beneficiary8.GetIdentifier()]
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = DbContext.BudgetAllowances.First();
            var localSubscription = DbContext.Subscriptions.First();
            var subscriptionBeneficiaries = DbContext.SubscriptionBeneficiaries.ToList();

            localBudgetAllowance.AvailableFund.Should().Be(60);
            localSubscription.Beneficiaries.Count.Should().Be(8);
            subscriptionBeneficiaries.Count().Should().Be(8);
        }

        [Fact]
        public async Task AssignSubscriptionToBeneficiariesOfType2()
        {
            var input = new AssignBeneficiariesToSubscription.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = subscription2.GetIdentifier(),
                Beneficiaries = [beneficiary5.GetIdentifier(), beneficiary6.GetIdentifier(), beneficiary7.GetIdentifier(), beneficiary8.GetIdentifier()]
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = DbContext.BudgetAllowances.First(x => x.Id == budgetAllowance2.Id);
            var localSubscription = DbContext.Subscriptions.First(x => x.Name == "Subscription 2");
            var subscriptionBeneficiaries = DbContext.SubscriptionBeneficiaries.ToList();

            localBudgetAllowance.AvailableFund.Should().Be(300);
            localSubscription.Beneficiaries.Count.Should().Be(4);
            subscriptionBeneficiaries.Count().Should().Be(4);
        }

        [Fact]
        public async Task AssignSubscriptionToBeneficiaries9()
        {
            var beneficiary9 = new Beneficiary()
            {
                SortOrder = 1,
                Organization = organization,
                Firstname = "John5",
                Lastname = "Doe5",
                Address = "123, example street",
                Email = "john5.doe5@example.com",
                Phone = "555-555-1234",
                BeneficiaryType = beneficiaryType2
            };
            beneficiary9.Subscriptions = new List<SubscriptionBeneficiary>() {
                new SubscriptionBeneficiary() {
                    Beneficiary = beneficiary9,
                    BeneficiaryType = beneficiary9.BeneficiaryType,
                    BudgetAllowance = budgetAllowance1,
                    Subscription = subscription2
                }
            };
            DbContext.Beneficiaries.Add(beneficiary9);
            DbContext.SaveChanges();

            var input = new AssignBeneficiariesToSubscription.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = subscription1.GetIdentifier(),
                Beneficiaries = [beneficiary9.GetIdentifier()]
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = DbContext.BudgetAllowances.First();
            var localSubscription = DbContext.Subscriptions.First();
            var subscriptionBeneficiaries = DbContext.SubscriptionBeneficiaries.ToList();

            localBudgetAllowance.AvailableFund.Should().Be(590);
            localSubscription.Beneficiaries.Count.Should().Be(1);
            subscriptionBeneficiaries.Count().Should().Be(2);
        }

        [Fact]
        public async Task AssignSubscriptionToZeroBeneficiary()
        {
            var input = new AssignBeneficiariesToSubscription.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = subscription1.GetIdentifier(),
                Beneficiaries = []
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = DbContext.BudgetAllowances.First();
            var localSubscription = DbContext.Subscriptions.First();
            var subscriptionBeneficiaries = DbContext.SubscriptionBeneficiaries.ToList();

            localBudgetAllowance.AvailableFund.Should().Be(700);
            localSubscription.Beneficiaries.Count.Should().Be(0);
            subscriptionBeneficiaries.Count().Should().Be(0);
        }

        [Fact]
        public async Task AssignSubscriptionWithMaxNumberOfPaymentToOneBeneficiary()
        {
            var input = new AssignBeneficiariesToSubscription.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = subscription3.GetIdentifier(),
                Beneficiaries = [beneficiary1.GetIdentifier(), beneficiary2.GetIdentifier()]
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = DbContext.BudgetAllowances.Last();
            var localSubscription = DbContext.Subscriptions.Last();
            var subscriptionBeneficiaries = DbContext.SubscriptionBeneficiaries.ToList();

            localBudgetAllowance.AvailableFund.Should().Be(500);
            localSubscription.Beneficiaries.Count.Should().Be(2);
            subscriptionBeneficiaries.Count().Should().Be(2);
        }

        [Fact]
        public async Task ThrowsIfOrganizationNotFound()
        {
            var input = new AssignBeneficiariesToSubscription.Input()
            {
                OrganizationId = Id.New<Organization>(123456),
                SubscriptionId = subscription1.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignBeneficiariesToSubscription.OrganizationNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfSubsriptionNotFound()
        {
            var input = new AssignBeneficiariesToSubscription.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = Id.New<Subscription>(123456),
                Beneficiaries = []
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignBeneficiariesToSubscription.SubscriptionNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionAlreadyExpiredException()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            subscription1.EndDate = today.Month > 1 ? new DateTime(today.Year, today.Month - 1, today.Day) : new DateTime(today.Year - 1, 12, today.Day);
            DbContext.SaveChanges();

            var input = new AssignBeneficiariesToSubscription.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = subscription1.GetIdentifier(),
                Beneficiaries = []
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignBeneficiariesToSubscription.SubscriptionAlreadyExpiredException>();
        }

        [Fact]
        public async Task ThrowsIfMissingBudgetAllowance()
        {
            DbContext.BudgetAllowances.Remove(budgetAllowance1);
            DbContext.SaveChanges();

            var input = new AssignBeneficiariesToSubscription.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = subscription1.GetIdentifier(),
                Beneficiaries = []
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignBeneficiariesToSubscription.MissingBudgetAllowanceException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotFound()
        {
            var input = new AssignBeneficiariesToSubscription.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = subscription1.GetIdentifier(),
                Beneficiaries = [Id.New<Beneficiary>(123456)]
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignBeneficiariesToSubscription.BeneficiaryNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryAlreadyGotSubscription()
        {
            var input = new AssignBeneficiariesToSubscription.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = subscription1.GetIdentifier(),
                Beneficiaries = [beneficiary1.GetIdentifier()]
            };
            await handler.Handle(input, CancellationToken.None);

            var inputError = new AssignBeneficiariesToSubscription.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = subscription1.GetIdentifier(),
                Beneficiaries = [beneficiary1.GetIdentifier()]
            };

            await F(() => handler.Handle(inputError, CancellationToken.None))
                .Should().ThrowAsync<AssignBeneficiariesToSubscription.BeneficiaryAlreadyGotSubscriptionException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryTypeNotInSubscription()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            var subscription3 = new Subscription()
            {
                Name = "Subscription 3",
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 50,
                        BeneficiaryType = beneficiaryType2
                    }
                },
                Project = project
            };
            DbContext.Subscriptions.Add(subscription3);

            var budgetAllowance3 = new BudgetAllowance()
            {
                AvailableFund = 700,
                Organization = organization,
                Subscription = subscription3,
                OriginalFund = 700
            };
            DbContext.BudgetAllowances.Add(budgetAllowance3);
            DbContext.SaveChanges();

            var input = new AssignBeneficiariesToSubscription.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = subscription3.GetIdentifier(),
                Beneficiaries = [beneficiary1.GetIdentifier()]
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignBeneficiariesToSubscription.BeneficiaryTypeNotInSubscriptionException>();
        }

        [Fact]
        public async Task ThrowsIfNotEnoughBudgetAllowance()
        {
            budgetAllowance1.AvailableFund = 1;
            DbContext.SaveChanges();

            var input = new AssignBeneficiariesToSubscription.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = subscription1.GetIdentifier(),
                Beneficiaries = [beneficiary1.GetIdentifier()]
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignBeneficiariesToSubscription.NotEnoughBudgetAllowanceException>();
        }
    }
}
