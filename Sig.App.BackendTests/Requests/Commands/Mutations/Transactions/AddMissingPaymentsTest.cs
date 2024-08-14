using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.BackgroundJobs;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Transactions
{
    public class AddMissingPaymentsTest : TestBase
    {
        private readonly AddMissingPayments handler;

        private readonly Market market;
        private readonly Project project;
        private readonly Card card;
        private readonly BeneficiaryType beneficiaryType;
        private readonly Beneficiary beneficiary;
        private readonly Organization organization;
        private readonly Subscription subscription;
        private readonly ProductGroup productGroup;
        private readonly BudgetAllowance budgetAllowance;

        public AddMissingPaymentsTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };

            market = new Market()
            {
                Name = "Market 1"
            };

            organization = new Organization()
            {
                Name = "Organization 1",
                Project = project
            };

            beneficiaryType = new BeneficiaryType()
            {
                Name = "Type 1",
                Project = project,
                Keys = "type1"
            };

            beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Organization = organization,
                BeneficiaryType = beneficiaryType
            };

            card = new Card()
            {
                Funds = new List<Fund>(),
                Status = CardStatus.Assigned,
                Project = project,
                Beneficiary = beneficiary,
                Transactions = new List<Transaction>()
            };

            productGroup = new ProductGroup()
            {
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1,
                Project = project
            };
            DbContext.ProductGroups.Add(productGroup);

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            subscription = new Subscription()
            {
                Name = "Subscription 1",
                Project = project,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 25,
                        ProductGroup = productGroup,
                        BeneficiaryType = beneficiaryType
                    },
                    new SubscriptionType()
                    {
                        Amount = 50
                    },
                    new SubscriptionType()
                    {
                        Amount = 100
                    }
                },
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                StartDate = new DateTime(today.Year, today.Month, 1),
                FundsExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(2),
                IsFundsAccumulable = true
            };

            budgetAllowance = new BudgetAllowance()
            {
                AvailableFund = 100,
                Organization = organization,
                Subscription = subscription,
                OriginalFund = 100
            };

            organization.Beneficiaries = new List<Beneficiary>() { beneficiary };
            organization.Project = project;

            beneficiary.Organization = organization;
            beneficiary.Card = card;
            beneficiary.Subscriptions = new List<SubscriptionBeneficiary>() { new SubscriptionBeneficiary() { Beneficiary = beneficiary, Subscription = subscription, BeneficiaryType = beneficiary.BeneficiaryType, BudgetAllowance = budgetAllowance } };

            project.Subscriptions = new List<Subscription>() { subscription };
            project.Organizations = new List<Organization>() { organization };
            project.Cards = new List<Card> { card };

            DbContext.Markets.Add(market);
            DbContext.Cards.Add(card);
            DbContext.Beneficiaries.Add(beneficiary);
            DbContext.Organizations.Add(organization);
            DbContext.Subscriptions.Add(subscription);
            DbContext.Projects.Add(project);
            DbContext.BudgetAllowances.Add(budgetAllowance);

            DbContext.SaveChanges();

            DbContext.ProjectMarkets.Add(new ProjectMarket() { MarketId = market.Id, ProjectId = project.Id });

            DbContext.SaveChanges();

            handler = new AddMissingPayments(NullLogger<AddMissingPayments>.Instance, DbContext, Clock, HttpContextAccessor, NullLogger<AddingFundToCard>.Instance);
        }

        [Fact]
        public async Task AddOneMissingPayment()
        {
            var input = new AddMissingPayments.Input()
            {
                Subscriptions = new List<Id>() { subscription.GetIdentifier() },
                BeneficiaryId = beneficiary.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = DbContext.BudgetAllowances.First();

            localBudgetAllowance.AvailableFund.Should().Be(75);
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotFound()
        {
            var input = new AddMissingPayments.Input()
            {
                BeneficiaryId = Id.New<Beneficiary>(123456),
                Subscriptions = new List<Id>() { subscription.GetIdentifier() },
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMissingPayments.BeneficiaryNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryDontHaveCardException()
        {
            beneficiary.Card = null;

            DbContext.SaveChanges();

            var input = new AddMissingPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                Subscriptions = new List<Id>() { subscription.GetIdentifier() },
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMissingPayments.BeneficiaryDontHaveCardException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryDontHaveThisSubscriptionException()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            var localSubscription = new Subscription()
            {
                Name = "Subscription test",
                Project = project,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 25,
                        ProductGroup = productGroup
                    },
                    new SubscriptionType()
                    {
                        Amount = 50
                    },
                    new SubscriptionType()
                    {
                        Amount = 100
                    }
                },
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                StartDate = new DateTime(today.Year, today.Month - 1, 1),
                FundsExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(2),
                IsFundsAccumulable = true
            };

            DbContext.Subscriptions.Add(localSubscription);
            DbContext.SaveChanges();

            var input = new AddMissingPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                Subscriptions = new List<Id>() { localSubscription.GetIdentifier() }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMissingPayments.BeneficiaryDontHaveThisSubscriptionException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionNotFound()
        {
            var input = new AddMissingPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                Subscriptions = new List<Id>() { Id.New<Subscription>(123456) }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMissingPayments.SubscriptionNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionExpired()
        {
            subscription.FundsExpirationDate = Clock.GetCurrentInstant().ToDateTimeUtc().AddDays(-1);
            subscription.EndDate = Clock.GetCurrentInstant().ToDateTimeUtc().AddDays(-2);
            DbContext.SaveChanges();

            var input = new AddMissingPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                Subscriptions = new List<Id>() { subscription.GetIdentifier() }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMissingPayments.SubscriptionExpiredException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionDontHaveMissedPaymentNotYetStarted()
        {
            subscription.StartDate = Clock.GetCurrentInstant().ToDateTimeUtc().AddDays(1);
            DbContext.SaveChanges();

            var input = new AddMissingPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                Subscriptions = new List<Id>() { subscription.GetIdentifier() }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMissingPayments.SubscriptionDontHaveMissedPaymentException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionDontHaveMissedPaymentAllPaymentReceived()
        {
            card.Transactions.Add(new SubscriptionAddingFundTransaction()
            {
                Amount = 25,
                AvailableFund = 25,
                Beneficiary = beneficiary,
                Card = card,
                CreatedAtUtc = Clock.GetCurrentInstant().ToDateTimeUtc(),
                ExpirationDate = subscription.GetExpirationDate(Clock),
                Organization = organization,
                ProductGroup = productGroup,
                Status = FundTransactionStatus.Actived,
                SubscriptionType = subscription.Types.First(),
            });

            DbContext.SaveChanges();

            var input = new AddMissingPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                Subscriptions = new List<Id>() { subscription.GetIdentifier() }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMissingPayments.SubscriptionDontHaveMissedPaymentException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionDontHaveMissedPaymentMaxPaymentReceived()
        {
            subscription.MaxNumberOfPayments = 1;
            subscription.StartDate = Clock.GetCurrentInstant().ToDateTimeUtc().AddMonths(-4);

            card.Transactions.Add(new SubscriptionAddingFundTransaction()
            {
                Amount = 25,
                AvailableFund = 25,
                Beneficiary = beneficiary,
                Card = card,
                CreatedAtUtc = Clock.GetCurrentInstant().ToDateTimeUtc(),
                ExpirationDate = subscription.GetExpirationDate(Clock),
                Organization = organization,
                ProductGroup = productGroup,
                Status = FundTransactionStatus.Actived,
                SubscriptionType = subscription.Types.First(),
            });

            DbContext.SaveChanges();

            var input = new AddMissingPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                Subscriptions = new List<Id>() { subscription.GetIdentifier() }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMissingPayments.SubscriptionDontHaveMissedPaymentException>();
        }

        [Fact]
        public async Task SubscriptionDontHaveEnoughtAvailableAmount()
        {
            budgetAllowance.AvailableFund = 0;
            DbContext.SaveChanges();

            var input = new AddMissingPayments.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                Subscriptions = new List<Id>() { subscription.GetIdentifier() }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMissingPayments.SubscriptionDontHaveEnoughtAvailableAmountException>();
        }
    }
}
