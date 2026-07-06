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
    public class AddSubscriptionPaymentTest : TestBase
    {
        private readonly AddSubscriptionPayment handler;

        private readonly Market market;
        private readonly Project project;
        private readonly Card card;
        private readonly BeneficiaryType beneficiaryType;
        private readonly Beneficiary beneficiary;
        private readonly Organization organization;
        private readonly Subscription subscription;
        private readonly ProductGroup productGroup;
        private readonly BudgetAllowance budgetAllowance;

        public AddSubscriptionPaymentTest()
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

            handler = new AddSubscriptionPayment(NullLogger<AddSubscriptionPayment>.Instance, DbContext, Clock, HttpContextAccessor, NullLogger<AddingFundToCard>.Instance);
        }

        [Fact]
        public async Task AddOneSubscriptionPayment()
        {
            var input = new AddSubscriptionPayment.Input()
            {
                SubscriptionId = subscription.GetIdentifier(),
                BeneficiaryId = beneficiary.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = DbContext.BudgetAllowances.First();

            localBudgetAllowance.AvailableFund.Should().Be(75);
        }

        // This payment is already "calculated" in the Budget of this beneficiary
        [Fact]
        public async Task AddOneSubscriptionPaymentForSubscriptionWithMaxPaymentFromBeneficiaryBudget()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            subscription.MaxNumberOfPayments = 1;
            subscription.StartDate = new DateTime(today.Year, today.Month, 1).AddMonths(-4);
            subscription.EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(6);
            subscription.FundsExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(7);

            DbContext.SaveChanges();

            var input = new AddSubscriptionPayment.Input()
            {
                SubscriptionId = subscription.GetIdentifier(),
                BeneficiaryId = beneficiary.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = DbContext.BudgetAllowances.First();

            localBudgetAllowance.AvailableFund.Should().Be(100);
        }

        // This payment is not "calculated" in the budget of this beneficiary
        [Fact]
        public async Task AddOneSubscriptionPaymentForSubscriptionWithMaxPaymentFromBudgetAllowance()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            subscription.MaxNumberOfPayments = 1;
            subscription.StartDate = new DateTime(today.Year, today.Month, 1).AddMonths(-4);
            subscription.EndDate = new DateTime(today.Year, today.Month, 1);
            subscription.FundsExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(1);

            DbContext.SaveChanges();

            var input = new AddSubscriptionPayment.Input()
            {
                SubscriptionId = subscription.GetIdentifier(),
                BeneficiaryId = beneficiary.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = DbContext.BudgetAllowances.First();

            localBudgetAllowance.AvailableFund.Should().Be(75);
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotFound()
        {
            var input = new AddSubscriptionPayment.Input()
            {
                BeneficiaryId = Id.New<Beneficiary>(123456),
                SubscriptionId = subscription.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddSubscriptionPayment.BeneficiaryNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryDontHaveCardException()
        {
            beneficiary.Card = null;

            DbContext.SaveChanges();

            var input = new AddSubscriptionPayment.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddSubscriptionPayment.BeneficiaryDontHaveCardException>();
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
                StartDate = new DateTime(today.Year, today.Month, 1),
                FundsExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(2),
                IsFundsAccumulable = true
            };

            DbContext.Subscriptions.Add(localSubscription);
            DbContext.SaveChanges();

            var input = new AddSubscriptionPayment.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = localSubscription.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddSubscriptionPayment.BeneficiaryDontHaveThisSubscriptionException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionNotFound()
        {
            var input = new AddSubscriptionPayment.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = Id.New<Subscription>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddSubscriptionPayment.SubscriptionNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionExpired()
        {
            subscription.FundsExpirationDate = Clock.GetCurrentInstant().ToDateTimeUtc().AddDays(-1);
            subscription.EndDate = Clock.GetCurrentInstant().ToDateTimeUtc().AddDays(-2);
            DbContext.SaveChanges();

            var input = new AddSubscriptionPayment.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddSubscriptionPayment.SubscriptionExpiredException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionNotYetStarted()
        {
            subscription.StartDate = Clock.GetCurrentInstant().ToDateTimeUtc().AddDays(1);
            DbContext.SaveChanges();

            var input = new AddSubscriptionPayment.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddSubscriptionPayment.SubscriptionMaxPaymentsReachedException>();
        }

        [Fact]
        public async Task AllowsPaymentWhenAllScheduledPaymentsReceivedButUnderMax()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            subscription.MaxNumberOfPayments = 6;
            subscription.StartDate = new DateTime(today.Year, today.Month, 1).AddMonths(-4);
            subscription.EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(6);
            subscription.FundsExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(7);

            for (var i = 0; i < 5; i++)
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
            }

            DbContext.SaveChanges();

            var input = new AddSubscriptionPayment.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().Count().Should().Be(6);
        }

        // CRCL-2526 (Partie 2) : sans max explicite, un versement au-delà du total programmé est
        // autorisé et débite l'enveloppe budgétaire.
        [Fact]
        public async Task AllowsPaymentWithoutExplicitMaxAndDebitsBudget()
        {
            subscription.EndDate = subscription.StartDate;
            DbContext.SaveChanges();

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

            var input = new AddSubscriptionPayment.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            // 1 versement existant + 1 nouveau, et l'enveloppe est débitée du montant (25).
            DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().Count().Should().Be(2);
            DbContext.BudgetAllowances.First().AvailableFund.Should().Be(75);
        }

        [Fact]
        public async Task ThrowsIfSubscriptionMaxPaymentsReached()
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

            var input = new AddSubscriptionPayment.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddSubscriptionPayment.SubscriptionMaxPaymentsReachedException>();
        }

        [Fact]
        public async Task ThrowsIfMaxNumberOfPaymentsOverrideIsReached()
        {
            subscription.StartDate = Clock.GetCurrentInstant().ToDateTimeUtc().AddMonths(-4);
            var subscriptionBeneficiary = beneficiary.Subscriptions.First();
            subscriptionBeneficiary.MaxNumberOfPaymentsOverride = 1;

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

            var input = new AddSubscriptionPayment.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddSubscriptionPayment.SubscriptionMaxPaymentsReachedException>();
        }

        [Fact]
        public async Task AllowsPaymentWhenOverrideIsHigherThanSubscriptionMax()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            subscription.MaxNumberOfPayments = 1;
            subscription.StartDate = new DateTime(today.Year, today.Month, 1).AddMonths(-4);
            subscription.EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(6);
            subscription.FundsExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(7);

            var subscriptionBeneficiary = beneficiary.Subscriptions.First();
            subscriptionBeneficiary.MaxNumberOfPaymentsOverride = 2;

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

            var input = new AddSubscriptionPayment.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            // override=2 > transactionCount=1, so no exception; budget already allocated
            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = DbContext.BudgetAllowances.First();
            localBudgetAllowance.AvailableFund.Should().Be(100);
        }

        [Fact]
        public async Task SubscriptionDontHaveEnoughtAvailableAmount()
        {
            budgetAllowance.AvailableFund = 0;
            DbContext.SaveChanges();

            var input = new AddSubscriptionPayment.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddSubscriptionPayment.SubscriptionDontHaveEnoughtAvailableAmountException>();
        }

        // CRCL-2526 : quand un abonnement verse dans plusieurs ProductGroups, chaque versement crée
        // une transaction par ProductGroup. On doit compter les versements réels, pas les transactions.
        [Fact]
        public async Task AllowsPaymentWithMultipleProductGroupsWhenRealPaymentsUnderMax()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            var secondProductGroup = AddSecondProductGroupToSubscription();

            subscription.MaxNumberOfPayments = 6;
            subscription.StartDate = new DateTime(today.Year, today.Month, 1).AddMonths(-4);
            subscription.EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(6);
            subscription.FundsExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(7);
            budgetAllowance.AvailableFund = 1000;

            // 3 versements réels x 2 ProductGroups = 6 transactions (mais 3 versements seulement).
            AddSubscriptionTransactions(3, secondProductGroup);

            DbContext.SaveChanges();

            var input = new AddSubscriptionPayment.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            // Avant le correctif : 6 transactions >= max(6) -> exception prématurée.
            // Après : 6 / 2 ProductGroups = 3 versements < 6 -> versement autorisé (2 nouvelles transactions).
            await handler.Handle(input, CancellationToken.None);

            DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().Count().Should().Be(8);
        }

        [Fact]
        public async Task ThrowsIfMaxPaymentsReachedWithMultipleProductGroups()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            var secondProductGroup = AddSecondProductGroupToSubscription();

            subscription.MaxNumberOfPayments = 3;
            subscription.StartDate = new DateTime(today.Year, today.Month, 1).AddMonths(-4);
            subscription.EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(6);
            subscription.FundsExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(7);
            budgetAllowance.AvailableFund = 1000;

            // 3 versements réels x 2 ProductGroups = 6 transactions = max atteint.
            AddSubscriptionTransactions(3, secondProductGroup);

            DbContext.SaveChanges();

            var input = new AddSubscriptionPayment.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddSubscriptionPayment.SubscriptionMaxPaymentsReachedException>();
        }

        private ProductGroup AddSecondProductGroupToSubscription()
        {
            var secondProductGroup = new ProductGroup()
            {
                Color = ProductGroupColor.Color_2,
                Name = "Product group 2",
                OrderOfAppearance = 2,
                Project = project
            };
            DbContext.ProductGroups.Add(secondProductGroup);

            subscription.Types.Add(new SubscriptionType()
            {
                Amount = 25,
                ProductGroup = secondProductGroup,
                BeneficiaryType = beneficiaryType
            });

            return secondProductGroup;
        }

        private void AddSubscriptionTransactions(int numberOfPayments, ProductGroup secondProductGroup)
        {
            var firstType = subscription.Types.First(x => x.BeneficiaryType == beneficiaryType && x.ProductGroup == productGroup);
            var secondType = subscription.Types.First(x => x.ProductGroup == secondProductGroup);

            for (var i = 0; i < numberOfPayments; i++)
            {
                foreach (var (type, group) in new[] { (firstType, productGroup), (secondType, secondProductGroup) })
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
                        ProductGroup = group,
                        Status = FundTransactionStatus.Actived,
                        SubscriptionType = type,
                    });
                }
            }
        }
    }
}
