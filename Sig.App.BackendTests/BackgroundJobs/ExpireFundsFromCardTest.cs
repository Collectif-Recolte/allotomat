using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.BackgroundJobs;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.BackgroundJobs
{
    public class ExpireFundsFromCardTest : TestBase
    {
        private readonly Project project;
        private readonly Card card;
        private readonly Beneficiary beneficiary;
        private readonly Organization organization;
        private readonly ExpireFundsFromCard job;
        private readonly ProductGroup productGroup;
        private readonly Subscription subscription1;

        public ExpireFundsFromCardTest()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            project = new Project()
            {
                Name = "Project 1"
            };

            organization = new Organization()
            {
                Name = "Organization 1",
                Project = project
            };

            beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Organization = organization
            };

            productGroup = new ProductGroup()
            {
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1,
                Project = project
            };
            DbContext.ProductGroups.Add(productGroup);

            var beneficiaryType1 = new BeneficiaryType()
            {
                Name = "Type 1",
                Project = project,
                Keys = "bliblou1"
            };

            subscription1 = new Subscription()
            {
                Name = "Subscription 1",
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(1),
                FundsExpirationDate = new DateTime(today.Year, today.Month, 2).AddMonths(2),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 50,
                        BeneficiaryType = beneficiaryType1,
                        ProductGroup = productGroup
                    }
                },
                Project = project
            };
            DbContext.Subscriptions.Add(subscription1);

            var budgetAllowance1 = new BudgetAllowance()
            {
                AvailableFund = 700,
                Organization = organization,
                Subscription = subscription1,
                OriginalFund = 700
            };
            DbContext.BudgetAllowances.Add(budgetAllowance1);

            card = new Card()
            {
                Funds = new List<Fund>(),
                Status = CardStatus.Assigned,
                Project = project,
                Beneficiary = beneficiary,
                Transactions = new List<Transaction>()
                {
                    new SubscriptionAddingFundTransaction()
                    {
                        Amount = 20,
                        AvailableFund = 0,
                        Status = FundTransactionStatus.Expired,
                        ExpirationDate = new DateTime(today.Year - 1, today.Month, today.Day),
                        ProductGroup = productGroup,
                        Beneficiary = beneficiary,
                        SubscriptionType = new SubscriptionType()
                        {
                            
                            Amount = 50,
                            ProductGroup = productGroup,
                            Subscription = subscription1,
                        }
                    },
                    new ManuallyAddingFundTransaction()
                    {
                        Amount = 30,
                        AvailableFund = 1,
                        Status = FundTransactionStatus.Actived,
                        ExpirationDate = new DateTime(today.Year - 1, today.Month, today.Day),
                        ProductGroup = productGroup,
                        Beneficiary = beneficiary,
                        Subscription = subscription1
                    },
                    new SubscriptionAddingFundTransaction()
                    {
                        Amount = 20,
                        AvailableFund = 10,
                        Status = FundTransactionStatus.Actived,
                        ExpirationDate = new DateTime(today.Year - 1, today.Month, today.Day),
                        ProductGroup = productGroup,
                        Beneficiary = beneficiary,
                        SubscriptionType = new SubscriptionType()
                        {
                            BeneficiaryType = new BeneficiaryType()
                            {
                                Name = "Type 2",
                                Project = project,
                                Keys = "bliblou2"
                            },
                            Amount = 50,
                            ProductGroup = productGroup,
                            Subscription = subscription1,
                        }
                    },
                    new ManuallyAddingFundTransaction()
                    {
                        Amount = 20,
                        AvailableFund = 20,
                        Status = FundTransactionStatus.Actived,
                        ExpirationDate = new DateTime(today.Year + 1, today.Month, today.Day),
                        ProductGroup = productGroup,
                        Beneficiary = beneficiary,
                        Subscription = subscription1
                    }
                }
            };

            card.Funds.Add(new Fund()
            {
                Amount = 30,
                Card = card,
                ProductGroup = productGroup
            });

            organization.Beneficiaries = new List<Beneficiary>() { beneficiary };
            organization.Project = project;

            beneficiary.Organization = organization;
            beneficiary.Card = card;

            project.Organizations = new List<Organization>() { organization };
            project.Cards = new List<Card> { card };

            DbContext.Cards.Add(card);
            DbContext.Beneficiaries.Add(beneficiary);
            DbContext.Organizations.Add(organization);
            DbContext.Projects.Add(project);

            DbContext.SaveChanges();

            job = new ExpireFundsFromCard(DbContext, Clock, NullLogger<ExpireFundsFromCard>.Instance);
        }

        [Fact]
        public async Task ExpireFundsFromCard()
        {
            await job.Run();

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(19);
            card.Transactions.Where(x => x.GetType() == typeof(SubscriptionAddingFundTransaction) && (x as SubscriptionAddingFundTransaction).Status == FundTransactionStatus.Expired).Should().HaveCount(2);
            card.Transactions.Where(x => x.GetType() == typeof(ManuallyAddingFundTransaction) && (x as ManuallyAddingFundTransaction).Status == FundTransactionStatus.Expired).Should().HaveCount(1);

            var transactionLogCreated = await DbContext.TransactionLogs.Where(x => x.Discriminator == TransactionLogDiscriminator.ExpireFundTransactionLog).ToListAsync();
            transactionLogCreated.Count.Should().Be(2);
            transactionLogCreated.Any(x => x.TotalAmount == 10).Should().BeTrue();
            transactionLogCreated.Any(x => x.TotalAmount == 1).Should().BeTrue();
        }

        [Fact]
        public async Task ExpireFundsFromCardAndReturnToBudgetAllowance()
        {
            await job.Run();

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(19);
            card.Transactions.Where(x => x.GetType() == typeof(SubscriptionAddingFundTransaction) && (x as SubscriptionAddingFundTransaction).Status == FundTransactionStatus.Expired).Should().HaveCount(2);
            card.Transactions.Where(x => x.GetType() == typeof(ManuallyAddingFundTransaction) && (x as ManuallyAddingFundTransaction).Status == FundTransactionStatus.Expired).Should().HaveCount(1);

            var transactionLogCreated = await DbContext.TransactionLogs.Where(x => x.Discriminator == TransactionLogDiscriminator.ExpireFundTransactionLog).ToListAsync();
            transactionLogCreated.Count.Should().Be(2);
            transactionLogCreated.Any(x => x.TotalAmount == 10).Should().BeTrue();
            transactionLogCreated.Any(x => x.TotalAmount == 1).Should().BeTrue();

            var budgetAllowance = DbContext.BudgetAllowances.First();
            budgetAllowance.AvailableFund.Should().Be(711);
        }

        // Builds a second project with its own organization, subscription and budget allowance, carrying one
        // ordinary active and expired transaction. Used to prove that a skip elsewhere in the same run does not
        // stop other projects from expiring normally.
        private (Card Card, ManuallyAddingFundTransaction Transaction, BudgetAllowance BudgetAllowance) AddOtherProjectWithNormalTransaction()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            var otherProject = new Project() { Name = "Project 2" };

            var otherOrganization = new Organization() { Name = "Organization 2", Project = otherProject };

            var otherBeneficiary = new Beneficiary() { Firstname = "Jane", Lastname = "Roe", Organization = otherOrganization };

            var otherProductGroup = new ProductGroup()
            {
                Color = ProductGroupColor.Color_1,
                Name = "Product group 2",
                OrderOfAppearance = 1,
                Project = otherProject
            };
            DbContext.ProductGroups.Add(otherProductGroup);

            var otherSubscription = new Subscription()
            {
                Name = "Subscription 2",
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(1),
                FundsExpirationDate = new DateTime(today.Year, today.Month, 2).AddMonths(2),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Project = otherProject
            };
            DbContext.Subscriptions.Add(otherSubscription);

            var otherBudgetAllowance = new BudgetAllowance()
            {
                AvailableFund = 200,
                Organization = otherOrganization,
                Subscription = otherSubscription,
                OriginalFund = 200
            };
            DbContext.BudgetAllowances.Add(otherBudgetAllowance);

            var otherTransaction = new ManuallyAddingFundTransaction()
            {
                Amount = 20,
                AvailableFund = 15,
                Status = FundTransactionStatus.Actived,
                ExpirationDate = new DateTime(today.Year - 1, today.Month, today.Day),
                ProductGroup = otherProductGroup,
                Beneficiary = otherBeneficiary,
                Subscription = otherSubscription
            };

            var otherCard = new Card()
            {
                Funds = new List<Fund>(),
                Status = CardStatus.Assigned,
                Project = otherProject,
                Beneficiary = otherBeneficiary,
                Transactions = new List<Transaction>() { otherTransaction }
            };
            otherCard.Funds.Add(new Fund() { Amount = 30, Card = otherCard, ProductGroup = otherProductGroup });

            otherBeneficiary.Card = otherCard;

            DbContext.Cards.Add(otherCard);
            DbContext.Beneficiaries.Add(otherBeneficiary);
            DbContext.Organizations.Add(otherOrganization);
            DbContext.Projects.Add(otherProject);

            DbContext.SaveChanges();

            return (otherCard, otherTransaction, otherBudgetAllowance);
        }

        // Builds a card in an organization that has no BudgetAllowance for subscription1 on the shared project,
        // the exact shape of the trigger described by the ticket: the Funds line is present, only the envelope
        // lookup fails.
        private (Card Card, ManuallyAddingFundTransaction Transaction) AddCardWithoutBudgetAllowance()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            var organizationWithoutEnvelope = new Organization() { Name = "Organization Without Envelope", Project = project };
            var beneficiaryWithoutEnvelope = new Beneficiary() { Firstname = "Jak", Lastname = "Orphan", Organization = organizationWithoutEnvelope };

            var orphanTransaction = new ManuallyAddingFundTransaction()
            {
                Amount = 10,
                AvailableFund = 3,
                Status = FundTransactionStatus.Actived,
                ExpirationDate = new DateTime(today.Year - 1, today.Month, today.Day),
                ProductGroup = productGroup,
                Beneficiary = beneficiaryWithoutEnvelope,
                Subscription = subscription1
            };

            var cardWithoutEnvelope = new Card()
            {
                Funds = new List<Fund>(),
                Status = CardStatus.Assigned,
                Project = project,
                Beneficiary = beneficiaryWithoutEnvelope,
                Transactions = new List<Transaction>() { orphanTransaction },
                ProgramCardId = 2,
                CardNumber = "orphan-card"
            };
            cardWithoutEnvelope.Funds.Add(new Fund() { Amount = 5, Card = cardWithoutEnvelope, ProductGroup = productGroup });

            beneficiaryWithoutEnvelope.Card = cardWithoutEnvelope;

            DbContext.Cards.Add(cardWithoutEnvelope);
            DbContext.Beneficiaries.Add(beneficiaryWithoutEnvelope);
            DbContext.Organizations.Add(organizationWithoutEnvelope);

            DbContext.SaveChanges();

            return (cardWithoutEnvelope, orphanTransaction);
        }

        // Builds a card with no Funds line at all for the product group its transaction targets, while its
        // organization (the shared fixture's organization) does have a valid budget allowance on subscription1.
        // Isolates the "Funds line absent" skip from the "budget allowance absent" one.
        private (Card Card, ManuallyAddingFundTransaction Transaction) AddCardWithoutFundsLine()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            var beneficiaryWithoutFunds = new Beneficiary() { Firstname = "Sam", Lastname = "NoFunds", Organization = organization };

            var fundlessTransaction = new ManuallyAddingFundTransaction()
            {
                Amount = 10,
                AvailableFund = 7,
                Status = FundTransactionStatus.Actived,
                ExpirationDate = new DateTime(today.Year - 1, today.Month, today.Day),
                ProductGroup = productGroup,
                Beneficiary = beneficiaryWithoutFunds,
                Subscription = subscription1
            };

            var cardWithoutFunds = new Card()
            {
                Funds = new List<Fund>(),
                Status = CardStatus.Assigned,
                Project = project,
                Beneficiary = beneficiaryWithoutFunds,
                Transactions = new List<Transaction>() { fundlessTransaction },
                ProgramCardId = 3,
                CardNumber = "fundless-card"
            };

            beneficiaryWithoutFunds.Card = cardWithoutFunds;

            DbContext.Cards.Add(cardWithoutFunds);
            DbContext.Beneficiaries.Add(beneficiaryWithoutFunds);

            DbContext.SaveChanges();

            return (cardWithoutFunds, fundlessTransaction);
        }

        [Fact]
        public async Task ExpireFundsFromCard_SkipsTransactionWhenBudgetAllowanceIsMissing()
        {
            // The orphan is inserted, and so queried, before the other project's transaction: if the
            // budget-allowance guard's `continue` ever regressed to a `break`, the other project's
            // transaction below would be left unprocessed by the time the loop reached it.
            var (orphanCard, orphanTransaction) = AddCardWithoutBudgetAllowance();
            var (otherCard, otherTransaction, otherBudgetAllowance) = AddOtherProjectWithNormalTransaction();

            await job.Run();

            // Skipped: state untouched, nothing decremented, nothing journaled.
            orphanCard.Funds.First().Amount.Should().Be(5);
            orphanTransaction.AvailableFund.Should().Be(3);
            orphanTransaction.Status.Should().Be(FundTransactionStatus.Actived);

            var transactionLogs = await DbContext.TransactionLogs
                .Where(x => x.Discriminator == TransactionLogDiscriminator.ExpireFundTransactionLog).ToListAsync();
            transactionLogs.Any(x => x.TotalAmount == 3).Should().BeFalse();

            // Another project queued after the orphan still expires normally: the skip is a `continue`,
            // not a `break`.
            otherCard.Funds.First().Amount.Should().Be(15);
            otherTransaction.AvailableFund.Should().Be(0);
            otherTransaction.Status.Should().Be(FundTransactionStatus.Expired);
            otherBudgetAllowance.AvailableFund.Should().Be(215);

            // A count, rather than only the other project's state: the base fixture's two transactions
            // and the other project's one (three in total) must all reach the log, whatever the query's
            // actual iteration order turns out to be.
            transactionLogs.Should().HaveCount(3);
        }

        [Fact]
        public async Task ExpireFundsFromCard_SkipsTransactionWhenFundsLineIsMissing()
        {
            var (otherCard, otherTransaction, otherBudgetAllowance) = AddOtherProjectWithNormalTransaction();
            var (fundlessCard, fundlessTransaction) = AddCardWithoutFundsLine();

            await job.Run();

            // Skipped: state untouched, nothing decremented, nothing journaled.
            fundlessCard.Funds.Should().BeEmpty();
            fundlessTransaction.AvailableFund.Should().Be(7);
            fundlessTransaction.Status.Should().Be(FundTransactionStatus.Actived);

            var transactionLogs = await DbContext.TransactionLogs
                .Where(x => x.Discriminator == TransactionLogDiscriminator.ExpireFundTransactionLog).ToListAsync();
            transactionLogs.Any(x => x.TotalAmount == 7).Should().BeFalse();

            // The organization's own envelope is only credited by the base fixture's own expiring transactions
            // (10 + 1, see ExpireFundsFromCardAndReturnToBudgetAllowance), never by the skipped one.
            var budgetAllowance = await DbContext.BudgetAllowances.FirstAsync(x => x.Organization == organization && x.Subscription == subscription1);
            budgetAllowance.AvailableFund.Should().Be(711);

            // Another project in the same run still expires normally.
            otherCard.Funds.First().Amount.Should().Be(15);
            otherTransaction.AvailableFund.Should().Be(0);
            otherTransaction.Status.Should().Be(FundTransactionStatus.Expired);
            otherBudgetAllowance.AvailableFund.Should().Be(215);
        }

        [Fact]
        public async Task ExpireFundsFromCard_LogsSkipReasonsAndCountForEachSkippedTransaction()
        {
            var (otherCard, otherTransaction, _) = AddOtherProjectWithNormalTransaction();
            var (orphanCard, orphanTransaction) = AddCardWithoutBudgetAllowance();
            var (fundlessCard, fundlessTransaction) = AddCardWithoutFundsLine();

            var logMessages = new List<(LogLevel Level, string Message)>();
            var loggerMock = new Mock<ILogger<ExpireFundsFromCard>>();
            loggerMock
                .Setup(x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()))
                .Callback(new InvocationAction(invocation =>
                {
                    var level = (LogLevel)invocation.Arguments[0];
                    var state = invocation.Arguments[2];
                    logMessages.Add((level, state.ToString()));
                }));

            var jobWithMockLogger = new ExpireFundsFromCard(DbContext, Clock, loggerMock.Object);
            await jobWithMockLogger.Run();

            var warnings = logMessages.Where(x => x.Level == LogLevel.Warning).Select(x => x.Message).ToList();

            warnings.Should().Contain(x =>
                x.Contains($"card {orphanCard.ProgramCardId} ({orphanCard.CardNumber})") &&
                x.Contains("has no budget allowance") &&
                x.Contains($"product group {productGroup.Id}"));
            warnings.Should().Contain(x =>
                x.Contains($"card {fundlessCard.ProgramCardId} ({fundlessCard.CardNumber})") &&
                x.Contains("has no Funds line") &&
                x.Contains($"product group {productGroup.Id}"));
            warnings.Should().ContainSingle(x => x.Contains("skipped this run"))
                .Which.Should().Contain("2 transaction(s) skipped this run");

            orphanTransaction.Status.Should().Be(FundTransactionStatus.Actived);
            fundlessTransaction.Status.Should().Be(FundTransactionStatus.Actived);
            otherTransaction.Status.Should().Be(FundTransactionStatus.Expired);
            otherCard.Funds.First().Amount.Should().Be(15);
        }
    }
}
