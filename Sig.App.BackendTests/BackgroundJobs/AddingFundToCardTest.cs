using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.BackgroundJobs;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Xunit;

namespace Sig.App.BackendTests.BackgroundJobs
{
    public class AddingFundToCardTest : TestBase
    {
        private readonly Project project;
        private readonly Card card;
        private readonly Beneficiary beneficiary;
        private readonly Organization organization;
        private readonly Subscription subscription;
        private readonly AddingFundToCard job;
        private readonly BeneficiaryType beneficiaryType;
        private readonly ProductGroup productGroup;

        public AddingFundToCardTest()
        {
            project = new Project()
            {
                Name = "Project 1"
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
                Keys = "bliblou"
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

            card = new Card()
            {
                Status = CardStatus.Assigned,
                Project = project,
                Beneficiary = beneficiary,
                Funds = new List<Fund>()
            };

            var fund = new Fund()
            {
                Amount = 20,
                ProductGroup = productGroup,
                Card = card
            };
            card.Funds.Add(fund);

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            subscription = new Subscription()
            {
                Name = "Subscription 1",
                Project = project,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        BeneficiaryType = beneficiaryType,
                        Amount = 25,
                        ProductGroup = productGroup
                    },
                    new SubscriptionType()
                    {
                        BeneficiaryType = new BeneficiaryType()
                        {
                            Name = "Type 2",
                            Project = project,
                            Keys = "bliblou2"
                        },
                        Amount = 50
                    },
                    new SubscriptionType()
                    {
                        BeneficiaryType = new BeneficiaryType()
                        {
                            Name = "Type 3",
                            Project = project,
                            Keys = "bliblou3"
                        },
                        Amount = 100
                    }
                },
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                StartDate = new DateTime(today.Year, today.Month, 1),
                FundsExpirationDate = new DateTime(today.Year, today.Month, 2).AddMonths(1)
            };

            organization.Beneficiaries = new List<Beneficiary>() { beneficiary };
            organization.Project = project;

            beneficiary.Organization = organization;
            beneficiary.Card = card;

            project.Subscriptions = new List<Subscription>() { subscription };
            project.Organizations = new List<Organization>() { organization };
            project.Cards = new List<Card> { card };

            DbContext.Cards.Add(card);
            DbContext.Beneficiaries.Add(beneficiary);
            DbContext.Organizations.Add(organization);
            DbContext.Subscriptions.Add(subscription);
            DbContext.Projects.Add(project);

            DbContext.SaveChanges();
            
            subscription.BudgetAllowances = new List<BudgetAllowance>()
            {
                new BudgetAllowance()
                {
                    Beneficiaries = new List<SubscriptionBeneficiary>()
                    {
                        new SubscriptionBeneficiary()
                        {
                            Beneficiary = beneficiary, 
                            Subscription = subscription, 
                            BeneficiaryType = beneficiary.BeneficiaryType
                        }
                    },
                    Organization = organization,
                    AvailableFund = 2500,
                    OriginalFund = 5000
                }
            };
            
            DbContext.SaveChanges();

            job = new AddingFundToCard(DbContext, Clock, NullLogger<AddingFundToCard>.Instance);
        }

        [Fact]
        public async Task AddFundToCard()
        {
            var budgetAllowance = DbContext.BudgetAllowances.First();
            var availableFundsInitially = budgetAllowance.AvailableFund;
            
            await job.Run(new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(45);
            
            budgetAllowance = DbContext.BudgetAllowances.First();
            var addedFunds = budgetAllowance.AvailableFund - availableFundsInitially;
            addedFunds.Should().Be(0);

            var transactionLog = await DbContext.TransactionLogs.FirstAsync(x =>
                x.Discriminator == TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog);
            transactionLog.TotalAmount.Should().Be(25);
        }

        [Fact]
        public async Task DontAddFundWithWrongMoment()
        {
            await job.Run(new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth });

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(20);
        }

        [Fact]
        public async Task AddFundToCardWithBothMoment()
        {
            await job.Run(new SubscriptionMonthlyPaymentMoment[2] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth, SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth });

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(45);
        }

        [Fact]
        public async Task AddFundWithCategoryRelatedToSubscription()
        {
            var beneficiaryType2 = new BeneficiaryType()
            {
                Name = "Type 2",
                Project = project,
                Keys = "bliblou2"
            };

            beneficiary.BeneficiaryType = beneficiaryType2;
            DbContext.BeneficiaryTypes.Add(beneficiaryType2);

            DbContext.SaveChanges();

            await job.Run(new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(45);
        }
        
        [Fact]
        public async Task RefundBudgetAllowanceWhenParticipantHasNoCards()
        {
            beneficiary.Card = null;
            beneficiary.CardId = null;
            var budgetAllowance = DbContext.BudgetAllowances.First();
            var availableFundsInitially = budgetAllowance.AvailableFund;
            
            DbContext.SaveChanges();
            
            await job.Run(new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(20);
            
            budgetAllowance = DbContext.BudgetAllowances.First();
            var addedFunds = budgetAllowance.AvailableFund - availableFundsInitially;
            addedFunds.Should().Be(25);
        }
    }
}
