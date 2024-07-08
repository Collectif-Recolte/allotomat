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
using System.Threading.Tasks;
using System.Threading;
using Xunit;
using GraphQL.Conventions;
using Sig.App.Backend.Extensions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Sig.App.Backend.DbModel.Entities.Profiles;

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
        
        private readonly ManuallyAddingFundTransaction initialTransaction4;

        private readonly PaymentTransaction initialPaymentTransaction1;
        private readonly PaymentTransaction initialPaymentTransaction2;

        private readonly ProductGroup productGroup;
        private readonly ProductGroup loyaltyProductgroup;

        public RefundTransactionTest()
        {
            var user = AddUser("test@example.com", UserType.PCAAdmin, password: "Abcd1234!!");
            user.Profile = new UserProfile()
            {
                FirstName = "Test",
                LastName = "Example"
            };
            DbContext.SaveChanges();

            SetLoggedInUser(user);

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
            ;
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

            initialTransaction4 = new ManuallyAddingFundTransaction()
            {
                TransactionUniqueId = "initialTransaction4",
                Amount = 20,
                Card = card,
                Beneficiary = beneficiary,
                OrganizationId = beneficiary.OrganizationId,
                AvailableFund = 20,
                ExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                Subscription = subscription,
                ProductGroup = productGroup
            };

            initialPaymentTransaction1 = new PaymentTransaction()
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
                TransactionUniqueId = "initialPaymentTransaction",
            };
            initialPaymentTransaction1.Transactions = new List<AddingFundTransaction>()
            {
                initialTransaction1,
                initialTransaction2
            };

            initialPaymentTransaction2 = new PaymentTransaction()
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
                        Amount = 20,
                        ProductGroup = productGroup
                    }
                },
                TransactionUniqueId = "initialPaymentTransaction",
            };
            initialPaymentTransaction2.Transactions = new List<AddingFundTransaction>()
            {
                initialTransaction3,
                initialTransaction4
            };
            initialPaymentTransaction2.PaymentTransactionAddingFundTransactions = new List<PaymentTransactionAddingFundTransaction>()
            {
                new PaymentTransactionAddingFundTransaction() {
                    AddingFundTransaction = initialTransaction3,
                    PaymentTransaction = initialPaymentTransaction2,
                    Amount = 10
                },
                new PaymentTransactionAddingFundTransaction()
                {
                    AddingFundTransaction = initialTransaction4,
                    PaymentTransaction = initialPaymentTransaction2,
                    Amount = 10
                }
            };

            card.Transactions = new List<Transaction>()
            {
                initialTransaction1,
                initialTransaction2,
                initialTransaction3,
                initialTransaction4,
                initialPaymentTransaction1,
                initialPaymentTransaction2
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
            DbContext.Transactions.Add(initialTransaction4);
            DbContext.Transactions.Add(initialPaymentTransaction1);
            DbContext.Transactions.Add(initialPaymentTransaction2);

            DbContext.SaveChanges();

            DbContext.ProjectMarkets.Add(new ProjectMarket() { MarketId = market.Id, ProjectId = project.Id });
            DbContext.SubscriptionBeneficiaries.Add(new SubscriptionBeneficiary() { Beneficiary = beneficiary, BeneficiaryType = beneficiary.BeneficiaryType, Subscription = subscription });

            DbContext.SaveChanges();

            handler = new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction(NullLogger<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction>.Instance, DbContext, mailer.Object, Clock, HttpContextAccessor, UserManager);
        }

        [Fact]
        public async Task CreateRefundTransactionWithFundFromMultipleSubscription()
        {
            var input = new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.Input()
            {
                InitialTransactionId = initialPaymentTransaction2.GetIdentifier(),
                Transactions = new List<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput>(),
                Password = "Abcd1234!!"
            };
            input.Transactions.Add(new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput()
            {
                Amount = 20,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await handler.Handle(input, CancellationToken.None);

            var localInitialPaymentTransaction = await DbContext.Transactions
                .Where(x => x.Id == initialPaymentTransaction2.Id)
                .OfType<PaymentTransaction>()
                .Include(x => x.Transactions)
                .Include(x => x.PaymentTransactionAddingFundTransactions)
                .Include(x => x.RefundTransactions).ThenInclude(x => x.RefundByProductGroups)
                .FirstAsync();

            localInitialPaymentTransaction.Amount.Should().Be(20);

            localInitialPaymentTransaction.Transactions.Count.Should().Be(2);
            localInitialPaymentTransaction.RefundTransactions.Count.Should().Be(1);
            localInitialPaymentTransaction.RefundTransactions.First().RefundByProductGroups.Count.Should().Be(1);

            var refundTransaction = localInitialPaymentTransaction.RefundTransactions.First();
            refundTransaction.Amount.Should().Be(20);
            refundTransaction.BeneficiaryId.Should().Be(beneficiary.Id);
            refundTransaction.CardId.Should().Be(card.Id);
            refundTransaction.InitialTransaction.Id.Should().Be(initialPaymentTransaction2.Id);
            refundTransaction.OrganizationId.Should().Be(organization.Id);
            refundTransaction.RefundByProductGroups.Count.Should().Be(1);

            var refundByProductGroup = refundTransaction.RefundByProductGroups.Where(x => x.ProductGroupId == productGroup.Id).First();
            refundByProductGroup.Amount.Should().Be(20);
            refundByProductGroup.ProductGroupId.Should().Be(productGroup.Id);
            refundByProductGroup.PaymentTransactionProductGroupId.Should().Be(localInitialPaymentTransaction.TransactionByProductGroups.First(x => x.ProductGroupId == productGroup.Id­­­­­).Id);
            refundByProductGroup.RefundTransactionId.Should().Be(refundTransaction.Id);

            var transactionByProductGroup = localInitialPaymentTransaction.TransactionByProductGroups.First(x => x.ProductGroupId == productGroup.Id);
            transactionByProductGroup.RefundAmount.Should().Be(20);
            transactionByProductGroup.Amount.Should().Be(20);

            var productGroupFund = await DbContext.Funds.Where(x => x.ProductGroupId == productGroup.Id).FirstOrDefaultAsync();
            productGroupFund.Amount.Should().Be(40);

            var paymentTransactionAddingFundTransactions = localInitialPaymentTransaction.PaymentTransactionAddingFundTransactions.ToList();
            paymentTransactionAddingFundTransactions.Count.Should().Be(2);
            paymentTransactionAddingFundTransactions.Where(x => x.AddingFundTransactionId == initialTransaction3.Id).First().Amount.Should().Be(10);
            paymentTransactionAddingFundTransactions.Where(x => x.AddingFundTransactionId == initialTransaction3.Id).First().RefundAmount.Should().Be(10);

            paymentTransactionAddingFundTransactions.Count.Should().Be(2);
            paymentTransactionAddingFundTransactions.Where(x => x.AddingFundTransactionId == initialTransaction4.Id).First().Amount.Should().Be(10);
            paymentTransactionAddingFundTransactions.Where(x => x.AddingFundTransactionId == initialTransaction4.Id).First().RefundAmount.Should().Be(10);
        }

        [Fact]
        public async Task CreateRefundTransactionForFullAmountFund()
        {
            var input = new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.Input()
            {
                InitialTransactionId = initialPaymentTransaction1.GetIdentifier(),
                Transactions = new List<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput>(),
                Password = "Abcd1234!!"
            };
            input.Transactions.Add(new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput()
            {
                Amount = 10,
                ProductGroupId = productGroup.GetIdentifier()
            });
            input.Transactions.Add(new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput()
            {
                Amount = 10,
                ProductGroupId = loyaltyProductgroup.GetIdentifier()
            });

            await handler.Handle(input, CancellationToken.None);

            var localInitialPaymentTransaction = await DbContext.Transactions
                .Where(x => x.Id == initialPaymentTransaction1.Id)
                .OfType<PaymentTransaction>()
                .Include(x => x.Transactions)
                .Include(x => x.RefundTransactions).ThenInclude(x => x.RefundByProductGroups)
                .FirstAsync();

            localInitialPaymentTransaction.Amount.Should().Be(20);

            localInitialPaymentTransaction.Transactions.Count.Should().Be(2);
            localInitialPaymentTransaction.RefundTransactions.Count.Should().Be(1);
            localInitialPaymentTransaction.RefundTransactions.First().RefundByProductGroups.Count.Should().Be(2);

            var refundTransaction = localInitialPaymentTransaction.RefundTransactions.First();
            refundTransaction.Amount.Should().Be(20);
            refundTransaction.BeneficiaryId.Should().Be(beneficiary.Id);
            refundTransaction.CardId.Should().Be(card.Id);
            refundTransaction.InitialTransaction.Id.Should().Be(initialPaymentTransaction1.Id);
            refundTransaction.OrganizationId.Should().Be(organization.Id);
            refundTransaction.RefundByProductGroups.Count.Should().Be(2);

            var refundByProductGroup = refundTransaction.RefundByProductGroups.Where(x => x.ProductGroupId == productGroup.Id).First();
            refundByProductGroup.Amount.Should().Be(10);
            refundByProductGroup.ProductGroupId.Should().Be(productGroup.Id);
            refundByProductGroup.PaymentTransactionProductGroupId.Should().Be(localInitialPaymentTransaction.TransactionByProductGroups.First(x => x.ProductGroupId == productGroup.Id­­­­­).Id);
            refundByProductGroup.RefundTransactionId.Should().Be(refundTransaction.Id);

            var refundByLoyaltyProductGroup = refundTransaction.RefundByProductGroups.Where(x => x.ProductGroupId == loyaltyProductgroup.Id).First();
            refundByLoyaltyProductGroup.Amount.Should().Be(10);
            refundByLoyaltyProductGroup.ProductGroupId.Should().Be(loyaltyProductgroup.Id);
            refundByLoyaltyProductGroup.PaymentTransactionProductGroupId.Should().Be(localInitialPaymentTransaction.TransactionByProductGroups.First(x => x.ProductGroupId == loyaltyProductgroup.Id­­­­­).Id);
            refundByLoyaltyProductGroup.RefundTransactionId.Should().Be(refundTransaction.Id);

            var transactionByProductGroup = localInitialPaymentTransaction.TransactionByProductGroups.First(x => x.ProductGroupId == productGroup.Id);
            transactionByProductGroup.RefundAmount.Should().Be(10);
            transactionByProductGroup.Amount.Should().Be(10);

            var productGroupFund = await DbContext.Funds.Where(x => x.ProductGroupId == productGroup.Id).FirstOrDefaultAsync();
            productGroupFund.Amount.Should().Be(30);
        }


        [Fact]
        public async Task CreateRefundTransactionWithProductGroupFund()
        {
            var input = new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.Input()
            {
                InitialTransactionId = initialPaymentTransaction1.GetIdentifier(),
                Transactions = new List<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput>(),
                Password = "Abcd1234!!"
            };
            input.Transactions.Add(new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput()
            {
                Amount = 10,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await handler.Handle(input, CancellationToken.None);

            var localInitialPaymentTransaction = await DbContext.Transactions
                .Where(x => x.Id == initialPaymentTransaction1.Id)
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
            refundTransaction.InitialTransaction.Id.Should().Be(initialPaymentTransaction1.Id);
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
                InitialTransactionId = initialPaymentTransaction1.GetIdentifier(),
                Transactions = new List<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput>(),
                Password = "Abcd1234!!"
            };
            input.Transactions.Add(new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput()
            {
                Amount = 10,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await handler.Handle(input, CancellationToken.None);

            var localInitialPaymentTransaction = await DbContext.Transactions
                .Where(x => x.Id == initialPaymentTransaction1.Id)
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
            refundTransaction.InitialTransaction.Id.Should().Be(initialPaymentTransaction1.Id);
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
                InitialTransactionId = initialPaymentTransaction1.GetIdentifier(),
                Transactions = new List<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput>(),
                Password = "Abcd1234!!"
            };
            input.Transactions.Add(new Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput()
            {
                Amount = 10,
                ProductGroupId = loyaltyProductgroup.GetIdentifier()
            });

            await handler.Handle(input, CancellationToken.None);

            var localInitialPaymentTransaction = await DbContext.Transactions
                .Where(x => x.Id == initialPaymentTransaction1.Id)
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
            refundTransaction.InitialTransaction.Id.Should().Be(initialPaymentTransaction1.Id);
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
                Transactions = new List<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput>(),
                Password = "Abcd1234!!"
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
                InitialTransactionId = initialPaymentTransaction1.GetIdentifier(),
                Transactions = new List<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput>(),
                Password = "Abcd1234!!"
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
                InitialTransactionId = initialPaymentTransaction1.GetIdentifier(),
                Transactions = new List<Backend.Requests.Commands.Mutations.Transactions.RefundTransaction.RefundTransactionsInput>(),
                Password = "Abcd1234!!"
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
