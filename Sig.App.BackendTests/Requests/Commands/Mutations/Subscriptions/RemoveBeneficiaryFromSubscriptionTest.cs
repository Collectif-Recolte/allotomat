using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities.Cards;
using Xunit;
using Sig.App.Backend.DbModel.Entities.Transactions;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Subscriptions
{
    public class RemoveBeneficiaryFromSubscriptionTest : TestBase
    {
        private readonly IRequestHandler<RemoveBeneficiaryFromSubscription.Input> handler;
        private readonly Subscription subscription;
        private readonly Beneficiary beneficiary;

        public RemoveBeneficiaryFromSubscriptionTest()
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

            var subscriptionType = new SubscriptionType()
            {
                Amount = 25,
                BeneficiaryType = beneficiaryType,
                ProductGroup = productGroup1
            };

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            subscription = new Subscription()
            {
                Name = "Subscription 1",
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<SubscriptionType>()
                {
                    subscriptionType,
                    new SubscriptionType()
                    {
                        Amount = 50,
                        ProductGroup = productGroup1
                    },
                    new SubscriptionType()
                    {
                        Amount = 100,
                        ProductGroup = productGroup2
                    }
                },
                Project = project
            };

            var budgetAllowance = new BudgetAllowance()
            {
                AvailableFund = 25,
                Organization = organization,
                Subscription = subscription,
                OriginalFund = 75
            };
            DbContext.BudgetAllowances.Add(budgetAllowance);

            subscription.Beneficiaries = new List<SubscriptionBeneficiary>() { new SubscriptionBeneficiary { Beneficiary = beneficiary, BeneficiaryType = beneficiaryType, Subscription = subscription, BudgetAllowance = budgetAllowance } };

            DbContext.Subscriptions.Add(subscription);

            DbContext.SaveChanges();

            handler = new RemoveBeneficiaryFromSubscription(NullLogger<RemoveBeneficiaryFromSubscription>.Instance, DbContext, Clock, HttpContextAccessor);
        }

        [Fact]
        public async Task RemoveBeneficiaryFromSubscription()
        {
            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiary = await DbContext.Beneficiaries.FirstAsync();
            var localSubscription = await DbContext.Subscriptions.FirstAsync();
            var localBudgetAllowance = await DbContext.BudgetAllowances.FirstAsync();
            var transactionLogCreated = await DbContext.TransactionLogs.AnyAsync(x => x.Discriminator == TransactionLogDiscriminator.RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog);

            localBeneficiary.Subscriptions.Should().HaveCount(0);
            localSubscription.Beneficiaries.Should().HaveCount(0);
            localBudgetAllowance.AvailableFund.Should().Be(50);
            transactionLogCreated.Should().Be(true);
        }

        [Fact]
        public async Task RemoveBeneficiaryFromSubscriptionWithMaximumPaymentCount()
        {
            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = 1;
            beneficiary.Card = new Card()
            {
                Transactions = new List<Transaction>() {
                    new SubscriptionAddingFundTransaction()
                    {
                        Amount = 25,
                        SubscriptionType = new SubscriptionType()
                        {
                            Subscription = subscription,
                            ProductGroup = subscription.Types.First(x => x.BeneficiaryType == beneficiary.BeneficiaryType).ProductGroup,
                            BeneficiaryType = beneficiary.BeneficiaryType
                        }
                    }
                }
            };

            DbContext.SaveChanges();

            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiary = await DbContext.Beneficiaries.FirstAsync();
            var localSubscription = await DbContext.Subscriptions.FirstAsync();
            var localBudgetAllowance = await DbContext.BudgetAllowances.FirstAsync();
            var transactionLogCreated = await DbContext.TransactionLogs.AnyAsync(x => x.Discriminator == TransactionLogDiscriminator.RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog);

            localBeneficiary.Subscriptions.Should().HaveCount(0);
            localSubscription.Beneficiaries.Should().HaveCount(0);
            localBudgetAllowance.AvailableFund.Should().Be(25);
            transactionLogCreated.Should().Be(true);
        }

        [Fact]
        public async Task ThrowsIfSubsriptionNotFound()
        {
            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = Id.New<Subscription>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveBeneficiaryFromSubscription.SubscriptionNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotFound()
        {
            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                BeneficiaryId = Id.New<Beneficiary>(123456),
                SubscriptionId = subscription.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveBeneficiaryFromSubscription.BeneficiaryNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotInSubscription()
        {
            var localProject = new Project();
            var localBeneficiary = new Beneficiary()
            {
                Firstname = "Jane",
                Lastname = "Doe",
                Address = "123, example street",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Organization = new Organization()
                {
                    Project = localProject
                }
            };
            DbContext.Beneficiaries.Add(localBeneficiary);
            DbContext.SaveChanges();

            var input = new RemoveBeneficiaryFromSubscription.Input()
            {
                SubscriptionId = subscription.GetIdentifier(),
                BeneficiaryId = localBeneficiary.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveBeneficiaryFromSubscription.BeneficiaryNotInSubscriptionException>();
        }
    }
}
