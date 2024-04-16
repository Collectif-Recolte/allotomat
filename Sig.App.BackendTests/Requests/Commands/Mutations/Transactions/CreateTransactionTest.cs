using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
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
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Transactions;
using Sig.App.Backend.Requests.Queries.Cards;
using Sig.App.Backend.Services.Mailer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Transactions
{
    public class CreateTransactionTest : TestBase
    {
        private readonly CreateTransaction handler;
        private Mock<IMailer> mailer;

        private readonly Market market;
        private readonly Project project;
        private readonly Project offPlatformProject;
        private readonly Card card;
        private readonly Card offPlatformCard;
        private readonly Beneficiary beneficiary;
        private readonly Beneficiary offPlatformBeneficiary;
        private readonly Organization organization;
        private readonly Organization offPlatformOrganization;
        private readonly Subscription subscription;
        private readonly Subscription subscription2;
        private readonly Transaction initialTransaction1;
        private readonly Transaction initialTransaction2;
        private readonly Transaction initialTransaction3;
        private readonly Transaction initialTransaction4;
        private readonly ProductGroup productGroup;

        public CreateTransactionTest()
        {
            mailer = new Mock<IMailer>();

            project = new Project()
            {
                Name = "Project 1"
            };
            
            offPlatformProject = new Project()
            {
                Name = "Project 1",
                AdministrationSubscriptionsOffPlatform = true
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

            offPlatformOrganization = new Organization()
            {
                Name = "Organization 2",
                Project = offPlatformProject
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
            
            offPlatformBeneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Deux",
                Organization = offPlatformOrganization,
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

            var loyaltyProductgroup = new ProductGroup()
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
                Beneficiary = beneficiary,
                CardNumber = "1234-5678-9012-3456"
            };

            offPlatformCard = new Card()
            {
                Funds = new List<Fund>(),
                Status = CardStatus.Assigned,
                Project = project,
                Beneficiary = offPlatformBeneficiary
            };

            card.Funds.Add(new Fund()
            {
                Amount = 40,
                Card = card,
                ProductGroup = productGroup
            });
            card.Funds.Add(new Fund()
            {
                Amount = 20,
                Card = card,
                ProductGroup = loyaltyProductgroup
            });

            offPlatformCard.Funds.Add(new Fund()
            {
                Amount = 20,
                Card = offPlatformCard,
                ProductGroup = productGroup
            });

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            subscription = new Subscription()
            {
                Name = "Subscription 1",
                Project = project,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType() { Amount = 25, ProductGroup = productGroup } , new SubscriptionType() { Amount = 50 } , new SubscriptionType() { Amount = 100 }
                },
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                StartDate = new DateTime(today.Year, today.Month, 1)
            };
            
            subscription2 = new Subscription()
            {
                Name = "Subscription 2",
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
                AvailableFund = 20,
                ExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                Subscription = subscription,
                ProductGroup = productGroup
            };

            initialTransaction2 = new LoyaltyAddingFundTransaction()
            {
                TransactionUniqueId = "initialTransaction2",
                Amount = 20,
                Card = card,
                AvailableFund = 20,
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
                Subscription = subscription2,
                ProductGroup = productGroup
            };
            
            initialTransaction4 = new OffPlatformAddingFundTransaction()
            {
                TransactionUniqueId = "initialTransaction4",
                Amount = 20,
                Card = offPlatformCard,
                Beneficiary = offPlatformBeneficiary,
                OrganizationId = offPlatformBeneficiary.OrganizationId,
                AvailableFund = 20,
                ExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                ProductGroup = productGroup
            };

            card.Transactions = new List<Transaction>()
            {
                initialTransaction1,
                initialTransaction2,
                initialTransaction3
            };

            offPlatformCard.Transactions = new List<Transaction>()
            {
                initialTransaction4
            };

            organization.Beneficiaries = new List<Beneficiary>() { beneficiary };
            organization.Project = project;

            offPlatformOrganization.Beneficiaries = new List<Beneficiary>() { offPlatformBeneficiary };
            offPlatformOrganization.Project = offPlatformProject;

            beneficiary.Organization = organization;
            beneficiary.Card = card;

            offPlatformBeneficiary.Organization = offPlatformOrganization;
            offPlatformBeneficiary.Card = offPlatformCard;

            project.Subscriptions = new List<Subscription>() { subscription };
            project.Organizations = new List<Organization>() { organization };
            project.Cards = new List<Card> { card };
            
            offPlatformProject.Organizations = new List<Organization>() { offPlatformOrganization };
            offPlatformProject.Cards = new List<Card> { offPlatformCard };
            
            DbContext.Markets.Add(market);
            DbContext.Cards.Add(card);
            DbContext.Cards.Add(offPlatformCard);
            DbContext.Beneficiaries.Add(beneficiary);
            DbContext.Beneficiaries.Add(offPlatformBeneficiary);
            DbContext.Organizations.Add(organization);
            DbContext.Organizations.Add(offPlatformOrganization);
            DbContext.Subscriptions.Add(subscription);
            DbContext.Projects.Add(project);
            DbContext.Projects.Add(offPlatformProject);
            DbContext.Transactions.Add(initialTransaction1);
            DbContext.Transactions.Add(initialTransaction2);
            DbContext.Transactions.Add(initialTransaction3);
            DbContext.Transactions.Add(initialTransaction4);

            DbContext.SaveChanges();

            DbContext.ProjectMarkets.Add(new ProjectMarket() { MarketId = market.Id, ProjectId = project.Id });
            DbContext.ProjectMarkets.Add(new ProjectMarket() { MarketId = market.Id, ProjectId = offPlatformProject.Id });
            DbContext.SubscriptionBeneficiaries.Add(new SubscriptionBeneficiary() { Beneficiary = beneficiary, BeneficiaryType = beneficiary.BeneficiaryType, Subscription = subscription });

            DbContext.SaveChanges();

            handler = new CreateTransaction(NullLogger<CreateTransaction>.Instance, DbContext, Mediator, mailer.Object, Clock, HttpContextAccessor);
        }

        [Fact]
        public async Task CreateTransaction()
        {
            SetupRequestHandler(new VerifyCardCanBeUsedInMarket(DbContext));

            var input = new CreateTransaction.Input()
            {
                MarketId = market.GetIdentifier(),
                Transactions = new List<CreateTransaction.TransactionInput>(),
                CardId = card.GetIdentifier(),
            };
            input.Transactions.Add(new CreateTransaction.TransactionInput()
            {
                Amount = 10,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await handler.Handle(input, CancellationToken.None);

            var transaction = await DbContext.Transactions.FirstAsync();

            transaction.CardId.Should().Be(card.Id);
            transaction.Amount.Should().Be(10);

            var transactionLog = await DbContext.TransactionLogs.FirstAsync();
            transactionLog.TransactionUniqueId.Should().Be(transaction.TransactionUniqueId);

            card.Funds.First().Amount.Should().Be(30);

            var initialTransaction = await DbContext.Transactions.OfType<ManuallyAddingFundTransaction>().FirstAsync(x => x.TransactionUniqueId == "initialTransaction1");
            initialTransaction.AvailableFund.Should().Be(10);
            initialTransaction.Amount.Should().Be(20);
            initialTransaction.Transactions.Count.Should().Be(1);
        }

        [Fact]
        public async Task CreateTransactionWithCardNumber()
        {
            SetupRequestHandler(new VerifyCardCanBeUsedInMarket(DbContext));

            var input = new CreateTransaction.Input()
            {
                MarketId = market.GetIdentifier(),
                Transactions = new List<CreateTransaction.TransactionInput>(),
                CardNumber = card.CardNumber
            };
            input.Transactions.Add(new CreateTransaction.TransactionInput()
            {
                Amount = 10,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await handler.Handle(input, CancellationToken.None);

            var transaction = await DbContext.Transactions.FirstAsync();

            transaction.CardId.Should().Be(card.Id);
            transaction.Amount.Should().Be(10);

            var transactionLog = await DbContext.TransactionLogs.FirstAsync();
            transactionLog.TransactionUniqueId.Should().Be(transaction.TransactionUniqueId);

            card.Funds.First().Amount.Should().Be(30);

            var initialTransaction = await DbContext.Transactions.OfType<ManuallyAddingFundTransaction>().FirstAsync(x => x.TransactionUniqueId == "initialTransaction1");
            initialTransaction.AvailableFund.Should().Be(10);
            initialTransaction.Amount.Should().Be(20);
            initialTransaction.Transactions.Count.Should().Be(1);
        }

        [Fact]
        public async Task CreateOffPlatformTransactionCreatesLog()
        {
            SetupRequestHandler(new VerifyCardCanBeUsedInMarket(DbContext));

            var input = new CreateTransaction.Input()
            {
                MarketId = market.GetIdentifier(),
                Transactions = new List<CreateTransaction.TransactionInput>(),
                CardId = offPlatformCard.GetIdentifier(),
            };
            input.Transactions.Add(new CreateTransaction.TransactionInput()
            {
                Amount = 10,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await handler.Handle(input, CancellationToken.None);

            var transaction = await DbContext.Transactions.FirstAsync();

            transaction.CardId.Should().Be(offPlatformCard.Id);
            transaction.Amount.Should().Be(10);

            var transactionLog = await DbContext.TransactionLogs.FirstAsync();
            transactionLog.TransactionUniqueId.Should().Be(transaction.TransactionUniqueId);

            offPlatformCard.Funds.First().Amount.Should().Be(10);
        }
        
        [Fact]
        public async Task CreateTransactionWithTwoBudgetAllowancesCreatesTwoTransactionLogs()
        {
            SetupRequestHandler(new VerifyCardCanBeUsedInMarket(DbContext));

            var input = new CreateTransaction.Input()
            {
                MarketId = market.GetIdentifier(),
                Transactions = new List<CreateTransaction.TransactionInput>(),
                CardId = card.GetIdentifier(),
            };
            input.Transactions.Add(new CreateTransaction.TransactionInput()
            {
                Amount = 40,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await handler.Handle(input, CancellationToken.None);

            var transaction = await DbContext.Transactions.OfType<PaymentTransaction>().FirstAsync();
            var transactionLogs = await DbContext.TransactionLogs.Where(x => x.TransactionUniqueId == transaction.TransactionUniqueId).ToListAsync();
            transactionLogs.Count.Should().Be(2);
        }
        
        [Fact]
        public async Task CreateTransactionEvenWithInvalidEmailAddress()
        {
            SetupRequestHandler(new VerifyCardCanBeUsedInMarket(DbContext));

            beneficiary.Email = "nil";
            await DbContext.SaveChangesAsync();
            
            var input = new CreateTransaction.Input()
            {
                MarketId = market.GetIdentifier(),
                Transactions = new List<CreateTransaction.TransactionInput>(),
                CardId = card.GetIdentifier(),
            };
            input.Transactions.Add(new CreateTransaction.TransactionInput()
            {
                Amount = 10,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await handler.Handle(input, CancellationToken.None);

            var transaction = await DbContext.Transactions.FirstAsync();

            transaction.CardId.Should().Be(card.Id);
            transaction.Amount.Should().Be(10);

            card.Funds.First().Amount.Should().Be(30);

            var initialTransaction = await DbContext.Transactions.OfType<ManuallyAddingFundTransaction>().FirstAsync(x => x.TransactionUniqueId == "initialTransaction1");
            initialTransaction.AvailableFund.Should().Be(10);
            initialTransaction.Amount.Should().Be(20);
            initialTransaction.Transactions.Count.Should().Be(1);
        }

        [Fact]
        public async Task CreateTransactionWithLoyaltyFund()
        {
            SetupRequestHandler(new VerifyCardCanBeUsedInMarket(DbContext));

            var input = new CreateTransaction.Input()
            {
                MarketId = market.GetIdentifier(),
                Transactions = new List<CreateTransaction.TransactionInput>(),
                CardId = card.GetIdentifier()
            };
            input.Transactions.Add(new CreateTransaction.TransactionInput()
            {
                Amount = 50,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await handler.Handle(input, CancellationToken.None);

            var transaction = await DbContext.Transactions.FirstAsync();

            transaction.CardId.Should().Be(card.Id);
            transaction.Amount.Should().Be(50);

            card.Funds.First().Amount.Should().Be(0);
            card.Funds.First(x => x.ProductGroup.Name == ProductGroupType.LOYALTY).Amount.Should().Be(10);

            var localInitialTransaction1 = await DbContext.Transactions.Where(x => x.Id == initialTransaction1.Id).Select(x => x as ManuallyAddingFundTransaction).FirstAsync();
            localInitialTransaction1.AvailableFund.Should().Be(0);
            localInitialTransaction1.Amount.Should().Be(20);
            localInitialTransaction1.Transactions.Count.Should().Be(1);

            var localInitialTransaction2 = await DbContext.Transactions.Where(x => x.Id == initialTransaction2.Id).Select(x => x as LoyaltyAddingFundTransaction).FirstAsync();
            localInitialTransaction2.AvailableFund.Should().Be(10);
            localInitialTransaction2.Amount.Should().Be(20);
            localInitialTransaction2.Transactions.Count.Should().Be(1);
        }

        [Fact]
        public async Task CreateTransactionAndChangeExpirationDate()
        {
            SetupRequestHandler(new VerifyCardCanBeUsedInMarket(DbContext));

            subscription.TriggerFundExpiration = FundsExpirationTrigger.NumberOfDays;
            subscription.NumberDaysUntilFundsExpire = 60;
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            card.Transactions = new List<Transaction>()
            {
                new SubscriptionAddingFundTransaction()
                {
                    TransactionUniqueId = "SubscriptionAddingFundTransaction1",
                    Amount = 20,
                    Card = beneficiary.Card,
                    Beneficiary = beneficiary,
                    OrganizationId = beneficiary.OrganizationId,
                    CreatedAtUtc = today,
                    ExpirationDate = today.AddMonths(4),
                    SubscriptionType = subscription.Types.First(),
                    AvailableFund = 20,
                    Status = FundTransactionStatus.Actived,
                    ProductGroup = productGroup
                }
            };

            DbContext.SaveChanges();

            var input = new CreateTransaction.Input()
            {
                MarketId = market.GetIdentifier(),
                Transactions = new List<CreateTransaction.TransactionInput>(),
                CardId = card.GetIdentifier(),
            };
            input.Transactions.Add(new CreateTransaction.TransactionInput()
            {
                Amount = 10,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await handler.Handle(input, CancellationToken.None);

            var transaction = await DbContext.Transactions.FirstAsync();

            transaction.CardId.Should().Be(card.Id);
            transaction.Amount.Should().Be(10);

            var transactionLog = await DbContext.TransactionLogs.FirstAsync();
            transactionLog.TransactionUniqueId.Should().Be(transaction.TransactionUniqueId);

            card.Funds.First().Amount.Should().Be(30);

            var initialTransaction = await DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().FirstAsync(x => x.TransactionUniqueId == "SubscriptionAddingFundTransaction1");
            initialTransaction.AvailableFund.Should().Be(10);
            initialTransaction.Amount.Should().Be(20);
            initialTransaction.Transactions.Count.Should().Be(1);
            initialTransaction.ExpirationDate.Should().Be(today.AddDays(60));
        }

        [Fact]
        public async Task ThrowsIfCardNotFound()
        {
            card.IsDisabled = true;

            DbContext.SaveChanges();

            var input = new CreateTransaction.Input()
            {
                MarketId = market.GetIdentifier(),
                Transactions = new List<CreateTransaction.TransactionInput>(),
                CardId = card.GetIdentifier(),
            };
            input.Transactions.Add(new CreateTransaction.TransactionInput()
            {
                Amount = 30,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateTransaction.CardIsDisabledException>();
        }

        [Fact]
        public async Task ThrowsIfCardDisabled()
        {
            var input = new CreateTransaction.Input()
            {
                MarketId = market.GetIdentifier(),
                Transactions = new List<CreateTransaction.TransactionInput>(),
                CardId = Id.New<Card>(123456)
            };
            input.Transactions.Add(new CreateTransaction.TransactionInput()
            {
                Amount = 30,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateTransaction.CardNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfCardNumberNotFound()
        {
            var input = new CreateTransaction.Input()
            {
                MarketId = market.GetIdentifier(),
                Transactions = new List<CreateTransaction.TransactionInput>(),
                CardNumber = "123456789"
            };
            input.Transactions.Add(new CreateTransaction.TransactionInput()
            {
                Amount = 30,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateTransaction.CardNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfMarketNotFound()
        {
            var input = new CreateTransaction.Input()
            {
                MarketId = Id.New<Market>(123456),
                Transactions = new List<CreateTransaction.TransactionInput>(),
                CardId = card.GetIdentifier(),
            };
            input.Transactions.Add(new CreateTransaction.TransactionInput()
            {
                Amount = 30,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateTransaction.MarketNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfCardCantBeUsedInMarket()
        {
            var localMarket = new Market()
            {
                Name = "Market 2"
            };
            DbContext.Markets.Add(localMarket);
            DbContext.SaveChanges();

            var input = new CreateTransaction.Input()
            {
                MarketId = localMarket.GetIdentifier(),
                Transactions = new List<CreateTransaction.TransactionInput>(),
                CardId = card.GetIdentifier()
            };
            input.Transactions.Add(new CreateTransaction.TransactionInput()
            {
                Amount = 30,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateTransaction.CardCantBeUsedInMarketException>();
        }

        [Fact]
        public async Task ThrowsIfNotEnoughtFund()
        {
            SetupRequestHandler(new VerifyCardCanBeUsedInMarket(DbContext));

            var input = new CreateTransaction.Input()
            {
                MarketId = market.GetIdentifier(),
                Transactions = new List<CreateTransaction.TransactionInput>(),
                CardId = card.GetIdentifier(),
            };
            input.Transactions.Add(new CreateTransaction.TransactionInput()
            {
                Amount = 70,
                ProductGroupId = productGroup.GetIdentifier()
            });

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateTransaction.NotEnoughtFundException>();
        }
    }
}
