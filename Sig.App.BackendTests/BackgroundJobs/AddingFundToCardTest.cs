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
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Helpers;
using NodaTime;

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
        private readonly SubscriptionBeneficiary subscriptionBeneficiary;

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
                Funds = new List<Fund>(),
                Transactions = new List<Transaction>()
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

            subscriptionBeneficiary = new SubscriptionBeneficiary()
            {
                Beneficiary = beneficiary,
                Subscription = subscription,
                BeneficiaryType = beneficiary.BeneficiaryType
            };

            subscription.BudgetAllowances = new List<BudgetAllowance>()
            {
                new BudgetAllowance()
                {
                    Beneficiaries = new List<SubscriptionBeneficiary>()
                    {
                        subscriptionBeneficiary
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
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 1, 0, 0));

            var budgetAllowance = DbContext.BudgetAllowances.First();
            var availableFundsInitially = budgetAllowance.AvailableFund;
            
            await job.Run("AddFundToCard", new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });

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
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 15, 0, 0));

            await job.Run("DontAddFundWithWrongMoment", new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth });

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(20);
        }

        [Fact]
        public async Task AddFundToCardWithBothMoment()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 1, 0, 0));

            await job.Run("AddFundToCardWithBothMoment", new SubscriptionMonthlyPaymentMoment[2] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth, SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth });

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(45);
        }

        [Fact]
        public async Task AddFundWithCategoryRelatedToSubscription()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 1, 0, 0));

            var beneficiaryType2 = new BeneficiaryType()
            {
                Name = "Type 2",
                Project = project,
                Keys = "bliblou2"
            };

            beneficiary.BeneficiaryType = beneficiaryType2;
            DbContext.BeneficiaryTypes.Add(beneficiaryType2);

            DbContext.SaveChanges();

            await job.Run("AddFundWithCategoryRelatedToSubscription", new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(45);
        }
        
        [Fact]
        public async Task RefundBudgetAllowanceWhenParticipantHasNoCards()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 1, 0, 0));

            beneficiary.Card = null;
            beneficiary.CardId = null;
            var budgetAllowance = DbContext.BudgetAllowances.First();
            var availableFundsInitially = budgetAllowance.AvailableFund;
            
            DbContext.SaveChanges();
            
            await job.Run("RefundBudgetAllowanceWhenParticipantHasNoCards", new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(20);
            
            budgetAllowance = DbContext.BudgetAllowances.First();
            var addedFunds = budgetAllowance.AvailableFund - availableFundsInitially;
            addedFunds.Should().Be(25);
        }

        [Fact]
        public async Task RefundBudgetAllowanceWhenParticipantMissAPayment()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 1, 0, 0));

            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = 2;
            subscriptionBeneficiary.RemainingAllocatedAmount = 25m;

            beneficiary.Card.Transactions.Add(new SubscriptionAddingFundTransaction()
            {
                TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(),
                Amount = 1,
                Card = beneficiary.Card,
                Beneficiary = beneficiary,
                OrganizationId = beneficiary.OrganizationId,
                CreatedAtUtc = today,
                ExpirationDate = today.AddMonths(1),
                SubscriptionType = subscription.Types.First(),
                AvailableFund = 1,
            });

            DbContext.SaveChanges();

            var budgetAllowance = DbContext.BudgetAllowances.First();
            var availableFundsInitially = budgetAllowance.AvailableFund;

            await job.Run("RefundBudgetAllowanceWhenParticipantMissAPayment", new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(20);

            budgetAllowance = DbContext.BudgetAllowances.First();
            var addedFunds = budgetAllowance.AvailableFund - availableFundsInitially;
            addedFunds.Should().Be(25);

            // CRCL-2606 : réservation relâchée dans l'enveloppe sans avoir été livrée. Le crédit de 25
            // et le décrément de la réservation doivent être exactement opposés.
            var localSubscriptionBeneficiary = DbContext.SubscriptionBeneficiaries.First();
            localSubscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(0m);
        }

        [Fact]
        public async Task DeliveringOnAPreMigrationRowKeepsTheReservationUnknown()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 1, 0, 0));

            // CRCL-2606 — Ligne antérieure à la migration : le versement est livré normalement, mais le
            // solde reste null. On ne décrémente pas depuis 0 : ça produirait une réservation négative
            // fictive, et pire, ça rendrait la ligne non-null donc faussement fiable au retrait.
            subscriptionBeneficiary.RemainingAllocatedAmount = null;
            DbContext.SaveChanges();

            await job.Run("DeliveringOnAPreMigrationRow", new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(45);

            DbContext.SubscriptionBeneficiaries.First().RemainingAllocatedAmount.Should().BeNull();
        }

        [Fact]
        public async Task DeliveringOneVersementConsumesTheReservationOnceNotOncePerProductGroup()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 1, 0, 0));

            // CRCL-2606 — Un versement génère une transaction par SubscriptionType. Il ne doit consommer
            // la réservation qu'UNE fois. Deuxième groupe de produits pour le même type de bénéficiaire :
            // 30 par versement (25 + 5), 3 versements réservés (90).
            var productGroup2 = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_2,
                Name = "Product group 2",
                OrderOfAppearance = 2
            };
            DbContext.ProductGroups.Add(productGroup2);
            subscription.Types.Add(new SubscriptionType()
            {
                BeneficiaryType = beneficiaryType,
                Amount = 5,
                ProductGroup = productGroup2
            });
            subscriptionBeneficiary.RemainingAllocatedAmount = 90m;

            DbContext.SaveChanges();

            await job.Run("DeliveringOneVersement", new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });

            // Les deux groupes ont bien été livrés : 2 transactions pour un seul versement.
            var deliveredTransactions = DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().ToList();
            deliveredTransactions.Should().HaveCount(2);

            // 90 - 30 = 60. Un décrément par transaction donnerait 30.
            var localSubscriptionBeneficiary = DbContext.SubscriptionBeneficiaries.First();
            localSubscriptionBeneficiary.RemainingAllocatedAmount.Should().Be(60m);
        }

        [Fact]
        public async Task DontRefundBudgetAllowanceWhenParticipantMissAPaymentButStillHaveTimeToGetAllPayment()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 1, 0, 0));

            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = 1;
            subscription.EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(2);

            var budgetAllowance = DbContext.BudgetAllowances.First();
            var availableFundsInitially = budgetAllowance.AvailableFund;

            beneficiary.Card.Transactions.Add(new SubscriptionAddingFundTransaction()
            {
                TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(),
                Amount = 1,
                Card = beneficiary.Card,
                Beneficiary = beneficiary,
                OrganizationId = beneficiary.OrganizationId,
                CreatedAtUtc = today,
                ExpirationDate = today.AddMonths(1),
                SubscriptionType = subscription.Types.First(),
                AvailableFund = 1
            });

            DbContext.SaveChanges();

            await job.Run("DontRefundBudgetAllowanceWhenParticipantMissAPaymentButStillHaveTimeToGetAllPayment", new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(20);

            budgetAllowance = DbContext.BudgetAllowances.First();
            var addedFunds = budgetAllowance.AvailableFund - availableFundsInitially;
            addedFunds.Should().Be(0);
        }

        [Fact]
        public async Task DontAddFundIfParticipantsDidntUseIsCardSinceLastPayment()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 1, 0, 0));

            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = 1;

            var budgetAllowance = DbContext.BudgetAllowances.First();
            var availableFundsInitially = budgetAllowance.AvailableFund;

            beneficiary.Card.Transactions.Add(new SubscriptionAddingFundTransaction()
            {
                TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(),
                Amount = 1,
                Card = beneficiary.Card,
                Beneficiary = beneficiary,
                OrganizationId = beneficiary.OrganizationId,
                CreatedAtUtc = today,
                ExpirationDate = today.AddMonths(1),
                SubscriptionType = subscription.Types.First(),
                AvailableFund = 1
            });

            DbContext.SaveChanges();

            await job.Run("DontAddFundIfParticipantsDidntUseIsCardSinceLastPayment", new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(20);
        }

        [Fact]
        public async Task AddFundIfParticipantsUseIsCardSinceLastPayment()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 1, 0, 0));

            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = 2;

            var budgetAllowance = DbContext.BudgetAllowances.First();
            var availableFundsInitially = budgetAllowance.AvailableFund;

            beneficiary.Card.Transactions.Add(new PaymentTransaction()
            {
                TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(),
                Amount = 1,
                Card = beneficiary.Card,
                Beneficiary = beneficiary,
                OrganizationId = beneficiary.OrganizationId,
                CreatedAtUtc = today
            });

            DbContext.SaveChanges();

            await job.Run("AddFundIfParticipantsUseIsCardSinceLastPayment", new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });

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
        public async Task AddFundIfParticipantHaveMaxNumberOfPaymentsOverride()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 1, 0, 0));

            var budgetAllowance = DbContext.BudgetAllowances.First();
            var availableFundsInitially = budgetAllowance.AvailableFund;

            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = 1;
            subscriptionBeneficiary.MaxNumberOfPaymentsOverride = 2;

            beneficiary.Card.Transactions.Add(new SubscriptionAddingFundTransaction()
            {
                TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(),
                Amount = 1,
                Card = beneficiary.Card,
                Beneficiary = beneficiary,
                OrganizationId = beneficiary.OrganizationId,
                CreatedAtUtc = today,
                ExpirationDate = today.AddMonths(1),
                SubscriptionType = subscription.Types.First(),
                AvailableFund = 1,
            });

            beneficiary.Card.Transactions.Add(new PaymentTransaction()
            {
                TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(),
                Amount = 1,
                Card = beneficiary.Card,
                Beneficiary = beneficiary,
                OrganizationId = beneficiary.OrganizationId,
                CreatedAtUtc = today
            });

            DbContext.SaveChanges();

            await job.Run("AddFundToCard", new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });

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
        public async Task AddFundToExistingSubscriptionBeneficiaryLoadsNavPropsWhenNull()
        {
            // Arrange: SubscriptionBeneficiary with only FK scalars — simulates a caller that forgot .Include()
            var partialSb = new SubscriptionBeneficiary
            {
                SubscriptionId = subscription.Id,
                BeneficiaryId = beneficiary.Id,
                BeneficiaryTypeId = beneficiaryType.Id
                // Subscription, Beneficiary, BeneficiaryType are intentionally null
            };

            var fundBefore = card.Funds.First().Amount;

            // Act
            await job.AddFundToExistingSubscriptionBeneficiary(partialSb);
            await DbContext.SaveChangesAsync();

            // Assert: fund was added despite null nav props (lazy-load kicked in)
            var updatedCard = DbContext.Cards.Include(x => x.Funds).First();
            updatedCard.Funds.First().Amount.Should().Be(fundBefore + 25);

            var transaction = DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().FirstOrDefault();
            transaction.Should().NotBeNull();
            transaction.Amount.Should().Be(25);
        }

        [Fact]
        public async Task DontAddFundIfParticipantHaveMaxNumberOfPaymentsOverrideAndMaxNumberOfTransaction()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            Clock.Reset(Instant.FromUtc(today.Year, today.Month, 1, 0, 0));

            var budgetAllowance = DbContext.BudgetAllowances.First();
            var availableFundsInitially = budgetAllowance.AvailableFund;

            subscription.IsSubscriptionPaymentBasedCardUsage = true;
            subscription.MaxNumberOfPayments = 1;
            subscriptionBeneficiary.MaxNumberOfPaymentsOverride = 2;

            beneficiary.Card.Transactions.Add(new SubscriptionAddingFundTransaction()
            {
                TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(),
                Amount = 1,
                Card = beneficiary.Card,
                Beneficiary = beneficiary,
                OrganizationId = beneficiary.OrganizationId,
                CreatedAtUtc = today,
                ExpirationDate = today.AddMonths(1),
                SubscriptionType = subscription.Types.First(),
                AvailableFund = 1,
            });

            beneficiary.Card.Transactions.Add(new SubscriptionAddingFundTransaction()
            {
                TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(),
                Amount = 1,
                Card = beneficiary.Card,
                Beneficiary = beneficiary,
                OrganizationId = beneficiary.OrganizationId,
                CreatedAtUtc = today,
                ExpirationDate = today.AddMonths(1),
                SubscriptionType = subscription.Types.First(),
                AvailableFund = 1,
            });

            DbContext.SaveChanges();

            await job.Run("AddFundToCard", new SubscriptionMonthlyPaymentMoment[1] { SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth });

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(20);
        }
    }
}
