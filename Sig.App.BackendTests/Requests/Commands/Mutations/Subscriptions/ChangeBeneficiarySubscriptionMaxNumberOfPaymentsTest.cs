using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Subscriptions
{
    public class ChangeBeneficiarySubscriptionMaxNumberOfPaymentsTest : TestBase
    {
        private readonly ChangeBeneficiarySubscriptionMaxNumberOfPayments handler;

        private readonly Subscription subscription;
        private readonly Beneficiary beneficiary;
        private readonly BudgetAllowance budgetAllowance;
        private readonly SubscriptionBeneficiary subscriptionBeneficiary;

        public ChangeBeneficiarySubscriptionMaxNumberOfPaymentsTest()
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

            var beneficiaryType = new BeneficiaryType()
            {
                Project = project,
                Keys = "Beneficiary type 1",
                Name = "Beneficiary type 1"
            };
            DbContext.BeneficiaryTypes.Add(beneficiaryType);

            beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Address = "123, example street",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                BeneficiaryType = beneficiaryType,
                Organization = organization
            };
            DbContext.Beneficiaries.Add(beneficiary);

            var productGroup = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };
            DbContext.ProductGroups.Add(productGroup);

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            subscription = new Subscription()
            {
                Name = "Subscription 1",
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(6),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                IsSubscriptionPaymentBasedCardUsage = false,
                MaxNumberOfPayments = 3,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 50,
                        BeneficiaryType = beneficiaryType,
                        ProductGroup = productGroup
                    }
                },
                Project = project
            };
            DbContext.Subscriptions.Add(subscription);

            budgetAllowance = new BudgetAllowance()
            {
                AvailableFund = 500,
                Organization = organization,
                Subscription = subscription,
                OriginalFund = 500
            };
            DbContext.BudgetAllowances.Add(budgetAllowance);

            subscriptionBeneficiary = new SubscriptionBeneficiary()
            {
                Beneficiary = beneficiary,
                Subscription = subscription,
                BeneficiaryType = beneficiaryType,
                BudgetAllowance = budgetAllowance
            };
            subscription.Beneficiaries = new List<SubscriptionBeneficiary>() { subscriptionBeneficiary };

            DbContext.SaveChanges();

            handler = new ChangeBeneficiarySubscriptionMaxNumberOfPayments(
                NullLogger<ChangeBeneficiarySubscriptionMaxNumberOfPayments>.Instance,
                DbContext,
                Clock);
        }

        [Fact]
        public async Task IncreaseMaxNumberOfPaymentsDeductsBudgetAllowance()
        {
            var input = new ChangeBeneficiarySubscriptionMaxNumberOfPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                MaxNumberOfPayments = 5
            };

            await handler.Handle(input, CancellationToken.None);

            var localSubscriptionBeneficiary = await DbContext.SubscriptionBeneficiaries
                .Include(x => x.BudgetAllowance)
                .FirstAsync(x => x.BeneficiaryId == beneficiary.Id && x.SubscriptionId == subscription.Id);

            // 5 - 3 = 2 additional payments * 50 = 100 deducted
            localSubscriptionBeneficiary.MaxNumberOfPaymentsOverride.Should().Be(5);
            localSubscriptionBeneficiary.BudgetAllowance.AvailableFund.Should().Be(400);
        }

        [Fact]
        public async Task IncreaseFromExistingOverrideDeductsOnlyDifference()
        {
            subscriptionBeneficiary.MaxNumberOfPaymentsOverride = 4;
            DbContext.SaveChanges();

            var input = new ChangeBeneficiarySubscriptionMaxNumberOfPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                MaxNumberOfPayments = 5
            };

            await handler.Handle(input, CancellationToken.None);

            var localSubscriptionBeneficiary = await DbContext.SubscriptionBeneficiaries
                .Include(x => x.BudgetAllowance)
                .FirstAsync(x => x.BeneficiaryId == beneficiary.Id && x.SubscriptionId == subscription.Id);

            // 5 - 4 = 1 additional payment * 50 = 50 deducted
            localSubscriptionBeneficiary.MaxNumberOfPaymentsOverride.Should().Be(5);
            localSubscriptionBeneficiary.BudgetAllowance.AvailableFund.Should().Be(450);
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotInSubscription()
        {
            var input = new ChangeBeneficiarySubscriptionMaxNumberOfPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = Id.New<Subscription>(123456),
                MaxNumberOfPayments = 5
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ChangeBeneficiarySubscriptionMaxNumberOfPayments.BeneficiaryNotInSubscriptionException>();
        }

        [Fact]
        public async Task ThrowsIfMaxNumberOfPaymentsIsEqualToCurrentMax()
        {
            var input = new ChangeBeneficiarySubscriptionMaxNumberOfPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                MaxNumberOfPayments = 3
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ChangeBeneficiarySubscriptionMaxNumberOfPayments.MaxNumberOfPaymentsMustBeGreaterThanCurrentException>();
        }

        [Fact]
        public async Task ThrowsIfMaxNumberOfPaymentsIsLessThanCurrentMax()
        {
            var input = new ChangeBeneficiarySubscriptionMaxNumberOfPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                MaxNumberOfPayments = 1
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ChangeBeneficiarySubscriptionMaxNumberOfPayments.MaxNumberOfPaymentsMustBeGreaterThanCurrentException>();
        }

        [Fact]
        public async Task ThrowsIfMaxNumberOfPaymentsIsLessThanCurrentOverride()
        {
            subscriptionBeneficiary.MaxNumberOfPaymentsOverride = 5;
            DbContext.SaveChanges();

            var input = new ChangeBeneficiarySubscriptionMaxNumberOfPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                MaxNumberOfPayments = 4
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ChangeBeneficiarySubscriptionMaxNumberOfPayments.MaxNumberOfPaymentsMustBeGreaterThanCurrentException>();
        }

        [Fact]
        public async Task ThrowsIfNotEnoughBudgetAllowance()
        {
            budgetAllowance.AvailableFund = 0;
            DbContext.SaveChanges();

            var input = new ChangeBeneficiarySubscriptionMaxNumberOfPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                MaxNumberOfPayments = 5
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ChangeBeneficiarySubscriptionMaxNumberOfPayments.NotEnoughBudgetAllowanceException>();
        }

        [Fact]
        public async Task ThrowsIfBudgetAllowanceCoversOnlyPartOfAdditionalPayments()
        {
            budgetAllowance.AvailableFund = 50; // enough for 1 payment but not 2
            DbContext.SaveChanges();

            var input = new ChangeBeneficiarySubscriptionMaxNumberOfPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                MaxNumberOfPayments = 5 // requires 2 additional payments = 100
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ChangeBeneficiarySubscriptionMaxNumberOfPayments.NotEnoughBudgetAllowanceException>();
        }

        [Fact]
        public async Task ThrowsIfEffectiveMaxNumberOfPaymentsIsLowerThanOverride()
        {
            var input = new ChangeBeneficiarySubscriptionMaxNumberOfPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                MaxNumberOfPayments = 7 // exceeds the 6 calendar payment slots remaining
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ChangeBeneficiarySubscriptionMaxNumberOfPayments.EffectiveMaxNumberOfPaymentsIsLowerThanOverrideException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionIsExpired()
        {
            subscription.EndDate = new DateTime(Clock.GetCurrentInstant().ToDateTimeUtc().Year - 1, 1, 1);
            DbContext.SaveChanges();

            var input = new ChangeBeneficiarySubscriptionMaxNumberOfPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                MaxNumberOfPayments = 5
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ChangeBeneficiarySubscriptionMaxNumberOfPayments.SubscriptionExpiredException>();
        }

        [Fact]
        public async Task IncreaseMaxNumberOfPaymentsSucceedsWhenSubscriptionIsPaymentBasedCardUsage()
        {
            // With IsSubscriptionPaymentBasedCardUsage=true, the old code used
            // subscriptionBeneficiary.GetPaymentRemaining() which was capped at
            // GetEffectiveMaxNumberOfPayments() (3), incorrectly blocking any increase
            // beyond the current max even when calendar slots allowed it.
            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            DbContext.SaveChanges();

            var input = new ChangeBeneficiarySubscriptionMaxNumberOfPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                MaxNumberOfPayments = 5 // > current max (3) but <= calendar payment slots (~6)
            };

            await handler.Handle(input, CancellationToken.None);

            var localSubscriptionBeneficiary = await DbContext.SubscriptionBeneficiaries
                .Include(x => x.BudgetAllowance)
                .FirstAsync(x => x.BeneficiaryId == beneficiary.Id && x.SubscriptionId == subscription.Id);

            // 5 - 3 = 2 additional payments * 50 = 100 deducted
            localSubscriptionBeneficiary.MaxNumberOfPaymentsOverride.Should().Be(5);
            localSubscriptionBeneficiary.BudgetAllowance.AvailableFund.Should().Be(400);
        }
    }
}
