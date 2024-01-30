using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Services.Mailer;
using Sig.App.Backend.DbModel.Enums;
using System.Collections.Generic;
using System;
using Sig.App.Backend.Requests.Commands.Mutations.Transactions;
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using GraphQL.Conventions;
using Sig.App.Backend.Extensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Requests.Queries.Cards;
using System.Linq;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Transactions
{
    public class RefundTransactionTest : TestBase
    {
        private readonly Backend.Requests.Commands.Mutations.Transactions.RefundTransaction handler;
        private Mock<IMailer> mailer;

        private readonly Market market;
        private readonly Project project;
        private readonly Card card;
        private readonly Beneficiary beneficiary;
        private readonly Organization organization;
        private readonly Subscription subscription;
        private readonly ManuallyAddingFundTransaction initialTransaction1;
        private readonly LoyaltyAddingFundTransaction initialTransaction2;
        private readonly ManuallyAddingFundTransaction initialTransaction3;
        private readonly PaymentTransaction initialPaymentTransaction;
        private readonly ProductGroup productGroup;
        private readonly ProductGroup loyaltyProductgroup;

        public RefundTransactionTest()
        {
            mailer = new Mock<IMailer>();

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

            var beneficiaryType = new BeneficiaryType()
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
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };
            DbContext.ProductGroups.Add(productGroup);

            loyaltyProductgroup = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_0,
                Name = ProductGroupType.LOYALTY,
                OrderOfAppearance = -1
            };
            DbContext.ProductGroups.Add(loyaltyProductgroup);

            card = new Card()
            {
                Funds = new List<Fund>(),
                Status = CardStatus.Assigned,
                Project = project,
                Beneficiary = beneficiary
            };

            card.Funds.Add(new Fund()
            {
                Amount = 20,
                Card = card,
                ProductGroup = productGroup
            });
            card.Funds.Add(new Fund()
            {
                Amount = 20,
                Card = card,
                ProductGroup = loyaltyProductgroup
            });

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            subscription = new Subscription()
            {
                Name = "Subscription 1",
                Project = project,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType() { Amount = 25 } , new SubscriptionType() { Amount = 50 } , new SubscriptionType() { Amount = 100 }
                },
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                StartDate = new DateTime(today.Year, today.Month, 1)
            };

            initialTransaction1 = new ManuallyAddingFundTransaction()
            {
                TransactionUniqueId = "initialTransaction1",
                Amount = 20,
                Card = card,
                Beneficiary = beneficiary,
                OrganizationId = beneficiary.OrganizationId,
                AvailableFund = 10,
                ExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                Subscription = subscription,
                ProductGroup = productGroup
            };

            initialTransaction2 = new LoyaltyAddingFundTransaction()
            {
                TransactionUniqueId = "initialTransaction2",
                Amount = 10,
                Card = card,
                AvailableFund = 10,
                ProductGroup = loyaltyProductgroup
            };

            initialTransaction3 = new ManuallyAddingFundTransaction()
            {
                TransactionUniqueId = "initialTransaction3",
                Amount = 20,
                Card = card,
                Beneficiary = beneficiary,
                OrganizationId = beneficiary.OrganizationId,
                AvailableFund = 20,
                ExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                Subscription = subscription,
                ProductGroup = productGroup
            };

            initialPaymentTransaction = new PaymentTransaction()
            {
                Amount = 20,
                Card = card,
                Beneficiary = beneficiary,
                Market = market,
                Organization = organization,
                TransactionByProductGroups = new List<PaymentTransactionProductGroup>()
                {
                    new PaymentTransactionProductGroup()
                    {
                        Amount = 10,
                        ProductGroup = productGroup
                    },
                    new PaymentTransactionProductGroup()
                    {
                        Amount = 10,
                        ProductGroup = loyaltyProductgroup
                    }
                },
                Transactions = new List<AddingFundTransaction>()
                {
                    initialTransaction1,
                    initialTransaction2
                },
                TransactionUniqueId = "initialPaymentTransaction",
            };

            card.Transactions = new List<Transaction>()
            {
                initialTransaction1,
                initialTransaction2,
                initialTransaction3,
                initialPaymentTransaction
            };

            organization.Beneficiaries = new List<Beneficiary>() { beneficiary };
            organization.Project = project;

            beneficiary.Organization = organization;
            beneficiary.Card = card;

            project.Subscriptions = new List<Subscription>() { subscription };
            project.Organizations = new List<Organization>() { organization };
            project.Cards = new List<Card> { card };

            DbContext.Markets.Add(market);
            DbContext.Cards.Add(card);
            DbContext.Beneficiaries.Add(beneficiary);
            DbContext.Organizations.Add(organization);
            DbContext.Subscriptions.Add(subscription);
            DbContext.Projects.Add(project);
            DbContext.Transactions.Add(initialTransaction1);
            DbContext.Transactions.Add(initialTransaction2);
            DbContext.Transactions.Add(initialTransaction3);
            DbContext.Transactions.Add(initialPaymentTransaction);

            DbContext.SaveChanges();

            DbContext.ProjectMarkets.Add(new ProjectMarket() { MarketId = market.Id, ProjectId = project.Id });
            DbContext.SubscriptionBeneficiaries.Add(new SubscriptionBeneficiary() { Beneficiary = beneficiary, BeneficiaryType = beneficiary.BeneficiaryType, Subscription = subscription });

            DbContext.SaveChanges();

            handler = new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction(NullLogger<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction>.Instance, DbContext, mailer.Object, Clock, HttpContextAccessor, UserManager);
        }

        [Fact]
        public async Task CreateRefundTransactionWithProductGroupFund()
        {
            var input = new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.Input()
            {
                InitialTransactionId = initialPaymentTransaction.GetIdentifier(),
                Transactions = new List<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput>()
            };
            input.Transactions.Add(new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput()
            {
                Amount = 10,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await handler.Handle(input, CancellationToken.None);

            var localInitialPaymentTransaction = await DbContext.Transactions
                .Where(x => x.Id == initialPaymentTransaction.Id)
                .OfType<PaymentTransaction>()
                .Include(x => x.Transactions)
                .Include(x => x.RefundTransactions).ThenInclude(x => x.RefundByProductGroups)
                .FirstAsync();

            localInitialPaymentTransaction.Amount.Should().Be(20);

            localInitialPaymentTransaction.Transactions.Count.Should().Be(2);
            localInitialPaymentTransaction.RefundTransactions.Count.Should().Be(1);

            var refundTransaction = localInitialPaymentTransaction.RefundTransactions.First();
            refundTransaction.Amount.Should().Be(10);
            refundTransaction.BeneficiaryId.Should().Be(beneficiary.Id);
            refundTransaction.CardId.Should().Be(card.Id);
            refundTransaction.InitialTransaction.Id.Should().Be(initialPaymentTransaction.Id);
            refundTransaction.OrganizationId.Should().Be(organization.Id);
            refundTransaction.RefundByProductGroups.Count.Should().Be(1);

            var refundByProductGroup = refundTransaction.RefundByProductGroups.First();
            refundByProductGroup.Amount.Should().Be(10);
            refundByProductGroup.ProductGroupId.Should().Be(productGroup.Id);
            refundByProductGroup.PaymentTransactionProductGroupId.Should().Be(localInitialPaymentTransaction.TransactionByProductGroups.First(x => x.ProductGroupId == productGroup.Id­­­­­).Id);
            refundByProductGroup.RefundTransactionId.Should().Be(refundTransaction.Id);

            var transactionByProductGroup = localInitialPaymentTransaction.TransactionByProductGroups.First(x => x.ProductGroupId == productGroup.Id);
            transactionByProductGroup.RefundAmount.Should().Be(10);
            transactionByProductGroup.Amount.Should().Be(10);

            var productGroupFund = await DbContext.Funds.Where(x => x.ProductGroupId == productGroup.Id).FirstOrDefaultAsync();
            productGroupFund.Amount.Should().Be(30);
        }

        [Fact]
        public async Task CreateRefundTransactionWithProductGroupExpiredFund()
        {
            initialTransaction1.Status = FundTransactionStatus.Expired;
            initialTransaction2.Status = FundTransactionStatus.Expired;
            await DbContext.SaveChangesAsync();

            var input = new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.Input()
            {
                InitialTransactionId = initialPaymentTransaction.GetIdentifier(),
                Transactions = new List<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput>()
            };
            input.Transactions.Add(new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput()
            {
                Amount = 10,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await handler.Handle(input, CancellationToken.None);

            var localInitialPaymentTransaction = await DbContext.Transactions
                .Where(x => x.Id == initialPaymentTransaction.Id)
                .OfType<PaymentTransaction>()
                .Include(x => x.Transactions)
                .Include(x => x.RefundTransactions).ThenInclude(x => x.RefundByProductGroups)
                .FirstAsync();

            localInitialPaymentTransaction.Amount.Should().Be(20);

            localInitialPaymentTransaction.Transactions.Count.Should().Be(2);
            localInitialPaymentTransaction.RefundTransactions.Count.Should().Be(1);

            var refundTransaction = localInitialPaymentTransaction.RefundTransactions.First();
            refundTransaction.Amount.Should().Be(10);
            refundTransaction.BeneficiaryId.Should().Be(beneficiary.Id);
            refundTransaction.CardId.Should().Be(card.Id);
            refundTransaction.InitialTransaction.Id.Should().Be(initialPaymentTransaction.Id);
            refundTransaction.OrganizationId.Should().Be(organization.Id);
            refundTransaction.RefundByProductGroups.Count.Should().Be(1);

            var refundByProductGroup = refundTransaction.RefundByProductGroups.First();
            refundByProductGroup.Amount.Should().Be(10);
            refundByProductGroup.ProductGroupId.Should().Be(productGroup.Id);
            refundByProductGroup.PaymentTransactionProductGroupId.Should().Be(localInitialPaymentTransaction.TransactionByProductGroups.First(x => x.ProductGroupId == productGroup.Id­­­­­).Id);
            refundByProductGroup.RefundTransactionId.Should().Be(refundTransaction.Id);

            var transactionByProductGroup = localInitialPaymentTransaction.TransactionByProductGroups.First(x => x.ProductGroupId == productGroup.Id);
            transactionByProductGroup.RefundAmount.Should().Be(10);
            transactionByProductGroup.Amount.Should().Be(10);

            var productGroupFund = await DbContext.Funds.Where(x => x.ProductGroupId == productGroup.Id).FirstOrDefaultAsync();
            productGroupFund.Amount.Should().Be(20);
        }

        [Fact]
        public async Task CreateRefundTransactionWithLoyaltyFund()
        {
            var input = new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.Input()
            {
                InitialTransactionId = initialPaymentTransaction.GetIdentifier(),
                Transactions = new List<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput>()
            };
            input.Transactions.Add(new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput()
            {
                Amount = 10,
                ProductGroupId = loyaltyProductgroup.GetIdentifier()
            });

            await handler.Handle(input, CancellationToken.None);

            var localInitialPaymentTransaction = await DbContext.Transactions
                .Where(x => x.Id == initialPaymentTransaction.Id)
                .OfType<PaymentTransaction>()
                .Include(x => x.Transactions)
                .Include(x => x.RefundTransactions).ThenInclude(x => x.RefundByProductGroups)
                .FirstAsync();

            localInitialPaymentTransaction.Amount.Should().Be(20);

            localInitialPaymentTransaction.Transactions.Count.Should().Be(2);
            localInitialPaymentTransaction.RefundTransactions.Count.Should().Be(1);

            var refundTransaction = localInitialPaymentTransaction.RefundTransactions.First();
            refundTransaction.Amount.Should().Be(10);
            refundTransaction.BeneficiaryId.Should().Be(beneficiary.Id);
            refundTransaction.CardId.Should().Be(card.Id);
            refundTransaction.InitialTransaction.Id.Should().Be(initialPaymentTransaction.Id);
            refundTransaction.OrganizationId.Should().Be(organization.Id);
            refundTransaction.RefundByProductGroups.Count.Should().Be(1);

            var refundByProductGroup = refundTransaction.RefundByProductGroups.First();
            refundByProductGroup.Amount.Should().Be(10);
            refundByProductGroup.ProductGroupId.Should().Be(loyaltyProductgroup.Id);
            refundByProductGroup.PaymentTransactionProductGroupId.Should().Be(localInitialPaymentTransaction.TransactionByProductGroups.First(x => x.ProductGroupId == loyaltyProductgroup.Id­­­­­).Id);
            refundByProductGroup.RefundTransactionId.Should().Be(refundTransaction.Id);

            var transactionByProductGroup = localInitialPaymentTransaction.TransactionByProductGroups.First(x => x.ProductGroupId == loyaltyProductgroup.Id);
            transactionByProductGroup.RefundAmount.Should().Be(10);
            transactionByProductGroup.Amount.Should().Be(10);

            var loyaltyFund = await DbContext.Funds.Where(x => x.ProductGroupId == loyaltyProductgroup.Id).FirstOrDefaultAsync();
            loyaltyFund.Amount.Should().Be(30);
        }

        [Fact]
        public async Task ThrowsIfInitialTransactionNotFound()
        {
            var input = new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.Input()
            {
                InitialTransactionId = Id.New<PaymentTransaction>(123456),
                Transactions = new List<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput>()
            };
            input.Transactions.Add(new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput()
            {
                Amount = 30,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.InitialTransactionNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfProductGroupNotFound()
        {
            var input = new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.Input()
            {
                InitialTransactionId = initialPaymentTransaction.GetIdentifier(),
                Transactions = new List<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput>()
            };
            input.Transactions.Add(new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput()
            {
                Amount = 30,
                ProductGroupId = Id.New<ProductGroup>(123456),
            });

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.ProductGroupNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfTooMuchRefund()
        {
            var input = new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.Input()
            {
                InitialTransactionId = initialPaymentTransaction.GetIdentifier(),
                Transactions = new List<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput>()
            };
            input.Transactions.Add(new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput()
            {
                Amount = 30,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.TooMuchRefundException>();
        }
    }
}
