using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NodaTime;
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
using Sig.App.Backend.Requests.Commands.Mutations.Projects;
using Sig.App.Backend.Requests.Commands.Mutations.Subscriptions;
using Sig.App.Backend.Services.Mailer;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Projects
{
    public class DeleteProjectTest : TestBase
    {
        private readonly IRequestHandler<DeleteProject.Input> handler;
        private Mock<IMailer> mailer;
        private readonly Project project;
        private readonly BeneficiaryType beneficiaryType;
        private readonly ProductGroup productGroup;

        public DeleteProjectTest()
        {
            project = new Project()
            {
                Name = "Project 1",

            };

            var market = new Market()
            {
                Name = "Market 1"
            };

            var organization = new Organization()
            {
                Name = "Organization 1",
                Project = project
            };

            beneficiaryType = new BeneficiaryType() {
                Keys = "type1",
                Name = "Type 1",
                Project = project
            };

            productGroup = new ProductGroup() {
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1,
                Project = project
            };

            var beneficiary = new Beneficiary() {
                Address = "123, rue de l'exemple",
                BeneficiaryType = beneficiaryType,
                Email = "john.doe@example.com",
                Firstname = "John",
                Lastname = "Doe",
                ID1 = "ID0001",
                ID2 = "ID0002",
                Notes = "Lorem ipsum ...",
                Organization = organization,
                Phone = "123-456-7890",
                PostalCode = "A1B2C3",
                SortOrder = 1
            };

            var card = new Card()
            {
                Beneficiary = beneficiary,
                CardNumber = "0000-1111-2222-3333",
                Status = CardStatus.Assigned,
                ProgramCardId = 1,
                Project = project
            };

            var fund = new Fund()
            {
                Amount = 20,
                ProductGroup = productGroup,
                Card = card
            };

            var subscription = new Subscription()
            {
                EndDate = new DateTime(2023, 12, 20),
                FundsExpirationDate = new DateTime(2023, 12, 20),
                Name = "Subscription 1",
                Project = project,
                StartDate = new DateTime(2023, 11, 20)
            };

            var subscriptionType = new SubscriptionType()
            {
                Amount = 20,
                BeneficiaryType = beneficiaryType,
                ProductGroup = productGroup,
                Subscription = subscription
            };

            var budgetAllowance = new BudgetAllowance()
            {
                AvailableFund = 100,
                Subscription = subscription,
                Organization = organization,
                OriginalFund = 200
            };

            var subscriptionBeneficiary = new SubscriptionBeneficiary() { Beneficiary = beneficiary, BeneficiaryType = beneficiaryType, Subscription = subscription, BudgetAllowance = budgetAllowance };
            budgetAllowance.Beneficiaries = new List<SubscriptionBeneficiary>() { subscriptionBeneficiary };

            var addingFundTransaction = new SubscriptionAddingFundTransaction()
            {
                Amount = 20,
                AvailableFund = 0,
                Beneficiary = beneficiary,
                Card = card,
                ExpirationDate = new DateTime(2023, 12, 20),
                Organization = organization,
                ProductGroup = productGroup,
                Status = FundTransactionStatus.Expired,
                TransactionUniqueId = "00000001",
                SubscriptionType = subscriptionType
            };
            var expireFundTransaction = new ExpireFundTransaction()
            {
                AddingFundTransaction = addingFundTransaction,
                Amount = 10,
                Beneficiary = beneficiary,
                Card = card,
                Organization = organization,
                TransactionUniqueId = "00000002"
            };
            var payment1Transaction = new PaymentTransaction()
            {
                Amount = 10,
                Beneficiary = beneficiary,
                Card = card,
                Market = market,
                Organization = organization,
                PaymentTransactionAddingFundTransactions = new List<PaymentTransactionAddingFundTransaction>(),
                Transactions = new List<AddingFundTransaction>(),
                TransactionUniqueId = "00000003"
            };

            var paymentTransactionAddingFundTransaction = new PaymentTransactionAddingFundTransaction() { AddingFundTransaction = addingFundTransaction, PaymentTransaction = payment1Transaction, Amount = 10 };

            payment1Transaction.Transactions.Add(addingFundTransaction);
            payment1Transaction.PaymentTransactionAddingFundTransactions.Add(paymentTransactionAddingFundTransaction);
            payment1Transaction.TransactionByProductGroups = new List<PaymentTransactionProductGroup>()
            {
                new PaymentTransactionProductGroup()
                {
                    Amount = 10,
                    PaymentTransaction = payment1Transaction,
                    ProductGroup = productGroup
                }
            };

            addingFundTransaction.Transactions = new List<PaymentTransaction>() { payment1Transaction };
            addingFundTransaction.ExpireFundTransaction = expireFundTransaction;

            project.Cards = new List<Card>() { card };
            project.BeneficiaryTypes = new List<BeneficiaryType>() { beneficiaryType };
            project.ProductGroups = new List<ProductGroup>() { productGroup };
            project.Markets = new List<ProjectMarket>()
            {
                new ProjectMarket()
                {
                    Market = market,
                    Project = project
                }
            };
            project.Organizations = new List<Organization>() { organization };
            project.Subscriptions = new List<Subscription>() { subscription };

            organization.Beneficiaries = new List<Beneficiary>() { beneficiary };
            organization.BudgetAllowances = new List<BudgetAllowance>() { budgetAllowance };

            beneficiaryType.Beneficiaries = new List<Beneficiary>() { beneficiary };

            productGroup.Funds = new List<Fund>() { fund };
            productGroup.Types = new List<SubscriptionType>() { subscriptionType };

            beneficiary.Card = card;
            beneficiary.Subscriptions = new List<SubscriptionBeneficiary>() { subscriptionBeneficiary };

            card.Funds = new List<Fund>() { fund };
            card.Transactions = new List<Transaction>()
            {
                addingFundTransaction,
                expireFundTransaction,
                payment1Transaction
            };

            subscription.Beneficiaries = new List<SubscriptionBeneficiary>() { subscriptionBeneficiary };
            subscription.BudgetAllowances = new List<BudgetAllowance>() { budgetAllowance };
            subscription.Types = new List<SubscriptionType>() { subscriptionType };

            DbContext.Projects.Add(project);
            DbContext.SaveChanges();

            mailer = new Mock<IMailer>();
            handler = new DeleteProject(NullLogger<DeleteProject>.Instance, DbContext, UserManager, mailer.Object, Mediator, Clock);
        }

        [Fact]
        public async Task DeleteTheProject()
        {
            var input = new DeleteProject.Input()
            {
                ProjectId = project.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var projectcount = await DbContext.Projects.CountAsync();
            projectcount.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfProjectNotFound()
        {
            var input = new DeleteProject.Input()
            {
                ProjectId = Id.New<Project>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteProject.ProjectNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfProjectHaveActiveSubscriptionNotFound()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            var nextMonthLastDay = today.AddMonths(1).AddDays(-1);

            var inputSubscription = new CreateSubscriptionInProject.Input()
            {
                ProjectId = project.GetIdentifier(),
                Name = "Subscription 1",
                StartDate = new LocalDate(today.Year, today.Month, 1),
                EndDate = new LocalDate(nextMonthLastDay.Year, nextMonthLastDay.Month, nextMonthLastDay.Day),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<CreateSubscriptionInProject.SubscriptionTypeInput>()
                {
                    new CreateSubscriptionInProject.SubscriptionTypeInput()
                    {
                        ProductGroupId = productGroup.GetIdentifier(),
                        Amount = 25,
                        BeneficiaryTypeId = beneficiaryType.GetIdentifier()
                    }
                }
            };

            await new CreateSubscriptionInProject(NullLogger<CreateSubscriptionInProject>.Instance, DbContext).Handle(inputSubscription, CancellationToken.None);

            var input = new DeleteProject.Input()
            {
                ProjectId = project.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteProject.ProjectCantHaveActiveSubscription>();
        }
    }
}
