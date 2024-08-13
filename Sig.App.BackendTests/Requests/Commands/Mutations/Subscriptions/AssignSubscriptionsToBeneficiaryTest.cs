using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Sig.App.Backend.BackgroundJobs;
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
    public class AssignSubscriptionsToBeneficiaryTest : TestBase
    {
        private readonly AssignSubscriptionsToBeneficiary handler;

        private readonly Project project;
        
        private readonly Organization organization;

        private readonly Subscription subscription1;
        private readonly Subscription subscription2;
        private readonly Subscription subscription3;

        private readonly Beneficiary beneficiary1;

        private readonly BeneficiaryType beneficiaryType1;
        private readonly BeneficiaryType beneficiaryType2;

        private readonly BudgetAllowance budgetAllowance1;
        private readonly BudgetAllowance budgetAllowance2;
        private readonly BudgetAllowance budgetAllowance3;

        public AssignSubscriptionsToBeneficiaryTest()
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

            handler = new AssignSubscriptionsToBeneficiary(NullLogger<AssignSubscriptionsToBeneficiary>.Instance, Clock, DbContext);

            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 4, 0, 0));
        }

        [Fact]
        public async Task AssignSubscriptionToAllBeneficiaries()
        {
            var input = new AssignSubscriptionsToBeneficiary.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                BeneficiaryId = beneficiary1.GetIdentifier(),
                Subscriptions = [subscription1.GetIdentifier(), subscription2.GetIdentifier(), subscription3.GetIdentifier()]
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowanceSubscription1 = DbContext.BudgetAllowances.First(x => x.SubscriptionId == subscription1.Id);
            var localBudgetAllowanceSubscription2 = DbContext.BudgetAllowances.First(x => x.SubscriptionId == subscription2.Id);
            var localBudgetAllowanceSubscription3 = DbContext.BudgetAllowances.First(x => x.SubscriptionId == subscription3.Id);
            var localSubscription1 = DbContext.Subscriptions.First(x => x.Id == subscription1.Id);
            var localSubscription2 = DbContext.Subscriptions.First(x => x.Id == subscription2.Id);
            var localSubscription3 = DbContext.Subscriptions.First(x => x.Id == subscription3.Id); ;

            localBudgetAllowanceSubscription1.AvailableFund.Should().Be(650);
            localBudgetAllowanceSubscription2.AvailableFund.Should().Be(650);
            localBudgetAllowanceSubscription3.AvailableFund.Should().Be(600);
            localSubscription1.Beneficiaries.Count.Should().Be(1);
            localSubscription2.Beneficiaries.Count.Should().Be(1);
            localSubscription3.Beneficiaries.Count.Should().Be(1);
        }

        [Fact]
        public async Task ThrowsIfOrganizationNotFound()
        {
            var input = new AssignSubscriptionsToBeneficiary.Input()
            {
                OrganizationId = Id.New<Organization>(123456),
                BeneficiaryId = beneficiary1.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignSubscriptionsToBeneficiary.OrganizationNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotFound()
        {
            var input = new AssignSubscriptionsToBeneficiary.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                BeneficiaryId = Id.New<Beneficiary>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignSubscriptionsToBeneficiary.BeneficiaryNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionAlreadyExpired()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            subscription1.EndDate = today.Month > 1 ? new DateTime(today.Year, today.Month - 1, today.Day) : new DateTime(today.Year - 1, 12, today.Day);
            DbContext.SaveChanges();

            var input = new AssignSubscriptionsToBeneficiary.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                BeneficiaryId = beneficiary1.GetIdentifier(),
                Subscriptions = [subscription1.GetIdentifier()]
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignSubscriptionsToBeneficiary.SubscriptionAlreadyExpiredException>();
        }

        [Fact]
        public async Task ThrowsIfMissingBudgetAllowance()
        {
            DbContext.BudgetAllowances.Remove(budgetAllowance1);
            DbContext.SaveChanges();

            var input = new AssignSubscriptionsToBeneficiary.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                BeneficiaryId = beneficiary1.GetIdentifier(),
                Subscriptions = [subscription1.GetIdentifier()]
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignSubscriptionsToBeneficiary.MissingBudgetAllowanceException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionNotFound()
        {
            var input = new AssignSubscriptionsToBeneficiary.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                BeneficiaryId = beneficiary1.GetIdentifier(),
                Subscriptions = [Id.New<Subscription>(123456)]
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignSubscriptionsToBeneficiary.SubscriptionNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryAlreadyGotSubscription()
        {
            var input = new AssignSubscriptionsToBeneficiary.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                BeneficiaryId = beneficiary1.GetIdentifier(),
                Subscriptions = [subscription1.GetIdentifier()]
            };
            await handler.Handle(input, CancellationToken.None);

            var inputError = new AssignSubscriptionsToBeneficiary.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                BeneficiaryId = beneficiary1.GetIdentifier(),
                Subscriptions = [subscription1.GetIdentifier()]
            };

            await F(() => handler.Handle(inputError, CancellationToken.None))
                .Should().ThrowAsync<AssignSubscriptionsToBeneficiary.BeneficiaryAlreadyGotSubscriptionException>();
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

            var input = new AssignSubscriptionsToBeneficiary.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                BeneficiaryId = beneficiary1.GetIdentifier(),
                Subscriptions = [subscription3.GetIdentifier()]
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignSubscriptionsToBeneficiary.BeneficiaryTypeNotInSubscriptionException>();
        }

        [Fact]
        public async Task ThrowsIfNotEnoughBudgetAllowance()
        {
            budgetAllowance1.AvailableFund = 1;
            DbContext.SaveChanges();

            var input = new AssignSubscriptionsToBeneficiary.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                BeneficiaryId = beneficiary1.GetIdentifier(),
                Subscriptions = [subscription1.GetIdentifier()]
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AssignSubscriptionsToBeneficiary.NotEnoughBudgetAllowanceException>();
        }
    }
}
