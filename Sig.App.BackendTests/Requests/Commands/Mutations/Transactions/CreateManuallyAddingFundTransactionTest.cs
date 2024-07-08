using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
    public class CreateManuallyAddingFundTransactionTest : TestBase
    {
        private readonly CreateManuallyAddingFundTransaction handler;

        private readonly Market market;
        private readonly Project project;
        private readonly Card card;
        private readonly BeneficiaryType beneficiaryType;
        private readonly Beneficiary beneficiary;
        private readonly Organization organization;
        private readonly Subscription subscription;
        private readonly ProductGroup productGroup;

        public CreateManuallyAddingFundTransactionTest()
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

            productGroup = new ProductGroup()
            {
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1,
                Project = project
            };
            DbContext.ProductGroups.Add(productGroup);

            card = new Card()
            {
                Funds = new List<Fund>(),
                Status = CardStatus.Assigned,
                Project = project,
                Beneficiary = beneficiary,
                Transactions = new List<Transaction>()
            };

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
                StartDate = new DateTime(today.Year, today.Month, 1),
                FundsExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                IsFundsAccumulable = true
            };

            var budgetAllowance = new BudgetAllowance()
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

            handler = new CreateManuallyAddingFundTransaction(NullLogger<CreateManuallyAddingFundTransaction>.Instance, DbContext, Clock, HttpContextAccessor);
        }

        [Fact]
        public async Task CreateTransactionManually()
        {
            var input = new CreateManuallyAddingFundTransaction.Input()
            {
                Amount = 10,
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                ProductGroupId = productGroup.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var transaction = await DbContext.Transactions.FirstAsync();

            transaction.CardId.Should().Be(card.Id);
            transaction.Amount.Should().Be(10);

            var transactionLog =
                await DbContext.TransactionLogs.FirstAsync(
                    x => x.TransactionUniqueId == transaction.TransactionUniqueId);
            transactionLog.TotalAmount.Should().Be(transaction.Amount);
            transactionLog.BeneficiaryIsOffPlatform.Should().Be(false);

            card.Funds.First().Amount.Should().Be(10);
        }

        [Fact]
        public async Task CreateTransactionManuallyForOffPlatformParticipant()
        {
            var project2 = new Project()
            {
                Name = "Project 2"
            };

            var organization2 = new Organization()
            {
                Name = "Organization 2",
                Project = project2
            };

            var beneficiary2 = new OffPlatformBeneficiary()
            {
                ID1 = "2",
                Firstname = "John",
                Lastname = "Doe",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Address = "123, Example Street",
                OrganizationId = organization2.Id,
                SortOrder = 0,
                IsActive = true,
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
            };
            beneficiary2.Subscriptions = new List<SubscriptionBeneficiary>() { new SubscriptionBeneficiary() { Beneficiary = beneficiary2, Subscription = subscription, BeneficiaryType = beneficiary.BeneficiaryType } };

            var productGroup2 = new ProductGroup()
            {
                Color = ProductGroupColor.Color_2,
                Name = "Product group 2",
                OrderOfAppearance = 1,
                Project = project2
            };
            DbContext.ProductGroups.Add(productGroup2);

            var card2 = new Card()
            {
                Funds = new List<Fund>(),
                Status = CardStatus.Assigned,
                Project = project2,
                Beneficiary = beneficiary2
            };

            organization2.Beneficiaries = new List<Beneficiary>() { beneficiary2 };
            organization2.Project = project2;

            beneficiary2.Organization = organization2;
            beneficiary2.Card = card2;

            project2.Organizations = new List<Organization>() { organization2 };
            project2.Cards = new List<Card> { card2 };

            DbContext.Cards.Add(card2);
            DbContext.Beneficiaries.Add(beneficiary2);
            DbContext.Organizations.Add(organization2);
            DbContext.Projects.Add(project2);

            DbContext.SaveChanges();

            var input = new CreateManuallyAddingFundTransaction.Input()
            {
                Amount = 10,
                BeneficiaryId = beneficiary2.GetIdentifier(),
                ProductGroupId = productGroup2.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var transaction = await DbContext.Transactions.FirstAsync();

            transaction.CardId.Should().Be(card2.Id);
            transaction.Amount.Should().Be(10);
            
            var transactionLog =
                await DbContext.TransactionLogs.FirstAsync(
                    x => x.TransactionUniqueId == transaction.TransactionUniqueId);
            transactionLog.TotalAmount.Should().Be(transaction.Amount);
            transactionLog.BeneficiaryIsOffPlatform.Should().Be(true);

            card2.Funds.First().Amount.Should().Be(10);
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotFound()
        {
            var input = new CreateManuallyAddingFundTransaction.Input()
            {
                Amount = 10,
                BeneficiaryId = Id.New<Beneficiary>(123456),
                SubscriptionId = subscription.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateManuallyAddingFundTransaction.BeneficiaryNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryDontHaveCardException()
        {
            var beneficiary2 = new Beneficiary()
            {
                Firstname = "John2",
                Lastname = "Doe",
                Organization = organization,
                BeneficiaryType = beneficiaryType
            };

            DbContext.Beneficiaries.Add(beneficiary2);
            DbContext.SaveChanges();

            var input = new CreateManuallyAddingFundTransaction.Input()
            {
                Amount = 10,
                BeneficiaryId = beneficiary2.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateManuallyAddingFundTransaction.BeneficiaryDontHaveCardException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionNotFoundException()
        {
            var input = new CreateManuallyAddingFundTransaction.Input()
            {
                Amount = 10,
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = Id.New<Subscription>(123456),
                ProductGroupId = productGroup.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateManuallyAddingFundTransaction.SubscriptionNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionExpiredException()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            var date = new DateTime(today.Year, today.Month, 1);
            var subscription2 = new Subscription()
            {
                Name = "Subscription 2",
                Project = project,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 25
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
                EndDate = date.AddMonths(-1),
                StartDate = date.AddMonths(-2),
                FundsExpirationDate = date.AddMonths(-1),
                IsFundsAccumulable = true
            };

            beneficiary.Subscriptions.Add(new SubscriptionBeneficiary() { Beneficiary = beneficiary, Subscription = subscription2, BeneficiaryType = beneficiary.BeneficiaryType });
            DbContext.Subscriptions.Add(subscription2);
            DbContext.SaveChanges();

            var input = new CreateManuallyAddingFundTransaction.Input()
            {
                Amount = 10,
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription2.GetIdentifier(),
                ProductGroupId = productGroup.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateManuallyAddingFundTransaction.SubscriptionExpiredException>();
        }
        
        [Fact]
        public async Task CreateTransactionWithSubscriptionExpiredAndFundsNotAccumulable()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            var date = new DateTime(today.Year, today.Month, 1);
            var subscription2 = new Subscription()
            {
                Name = "Subscription 2",
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
                        Amount = 50,
                        ProductGroup = productGroup
                    },
                    new SubscriptionType()
                    {
                        Amount = 100,
                        ProductGroup = productGroup
                    }
                },
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                EndDate = date.AddMonths(-1),
                StartDate = date.AddMonths(-2),
                FundsExpirationDate = date.AddMonths(-1),
                IsFundsAccumulable = false
            };
            
            var budgetAllowance2 = new BudgetAllowance()
            {
                AvailableFund = 100,
                Organization = organization,
                Subscription = subscription2,
                OriginalFund = 100
            };

            beneficiary.Subscriptions.Add(new SubscriptionBeneficiary() { Beneficiary = beneficiary, Subscription = subscription2, BudgetAllowance = budgetAllowance2, BeneficiaryType = beneficiary.BeneficiaryType });
            
            DbContext.Subscriptions.Add(subscription2);
            DbContext.BudgetAllowances.Add(budgetAllowance2);
            DbContext.SaveChanges();

            var input = new CreateManuallyAddingFundTransaction.Input()
            {
                Amount = 10,
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription2.GetIdentifier(),
                ProductGroupId = productGroup.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);
            
            var transaction = await DbContext.Transactions.FirstAsync();

            transaction.CardId.Should().Be(card.Id);
            transaction.Amount.Should().Be(10);

            card.Funds.First().Amount.Should().Be(10);
        }

        [Fact]
        public async Task ThrowsIfSubscriptionDontHaveBudgetAllowance()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            var subscription2 = new Subscription()
            {
                Name = "Subscription 2",
                Project = project,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 25
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
                FundsExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
            };

            beneficiary.Subscriptions.Add(new SubscriptionBeneficiary() { Beneficiary = beneficiary, Subscription = subscription2, BeneficiaryType = beneficiary.BeneficiaryType });
            DbContext.Subscriptions.Add(subscription2);
            DbContext.SaveChanges();

            var input = new CreateManuallyAddingFundTransaction.Input()
            {
                Amount = 10,
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription2.GetIdentifier(),
                ProductGroupId = productGroup.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateManuallyAddingFundTransaction.SubscriptionDontHaveBudgetAllowance>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionDontHaveEnoughtAvailableAmount()
        {
            var input = new CreateManuallyAddingFundTransaction.Input()
            {
                Amount = 101,
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                ProductGroupId = productGroup.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateManuallyAddingFundTransaction.SubscriptionDontHaveEnoughtAvailableAmount>();
        }

        [Fact]
        public async Task ThrowsIfAvailableFundCantBeLessThanZero()
        {
            var input = new CreateManuallyAddingFundTransaction.Input()
            {
                Amount = -99,
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                ProductGroupId = productGroup.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateManuallyAddingFundTransaction.AvailableFundCantBeLessThanZero>();
        }

        [Fact]
        public async Task CreateNegatifTransaction()
        {
            var fund = new Fund()
            {
                Amount = 10,
                ProductGroupId = productGroup.Id
            };
            card.Funds.Add(fund);

            var saft1 = new SubscriptionAddingFundTransaction()
            {
                Amount = 25,
                AvailableFund = 5,
                Beneficiary = beneficiary,
                Card = card,
                CreatedAtUtc = Clock.GetCurrentInstant().ToDateTimeUtc(),
                ExpirationDate = Clock.GetCurrentInstant().ToDateTimeUtc(),
                Organization = organization,
                ProductGroup = productGroup,
                Status = FundTransactionStatus.Actived,
                SubscriptionType = subscription.Types.First(),
            };
            
            var saft2 = new SubscriptionAddingFundTransaction()
            {
                Amount = 5,
                AvailableFund = 5,
                Beneficiary = beneficiary,
                Card = card,
                CreatedAtUtc = Clock.GetCurrentInstant().ToDateTimeUtc(),
                ExpirationDate = subscription.GetExpirationDate(Clock),
                Organization = organization,
                ProductGroup = productGroup,
                Status = FundTransactionStatus.Actived,
                SubscriptionType = subscription.Types.First(),
            };

            card.Transactions.Add(saft1);
            DbContext.Transactions.Add(saft1);
            card.Transactions.Add(saft2);
            DbContext.Transactions.Add(saft2);

            DbContext.Funds.Add(fund);
            DbContext.SaveChanges();

            var input = new CreateManuallyAddingFundTransaction.Input()
            {
                Amount = -8,
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                ProductGroupId = productGroup.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var transaction = await DbContext.Transactions.OfType<ManuallyAddingFundTransaction>().Include(x => x.AffectedNegativeFundTransactions).LastAsync();

            transaction.AffectedNegativeFundTransactions.Should().HaveCount(2);
            transaction.Amount.Should().Be(-8);
            var saft1Locale = await DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().Where(x => x.Amount == 25).FirstAsync();
            saft1Locale.AvailableFund.Should().Be(0);
            var saft2Locale = await DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().Where(x => x.Amount == 5).LastAsync();
            saft2Locale.AvailableFund.Should().Be(2);

            var localFund = await DbContext.Funds.FirstAsync();
            localFund.Amount.Should().Be(2);
        }
    }
}
