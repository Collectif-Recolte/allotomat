using FluentAssertions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Requests.Queries.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Queries.Transactions
{
    public class GetRemovableFundTest : TestBase
    {
        private readonly GetRemovableFund handler;

        private readonly Project project;
        private readonly Organization organization;
        private readonly Beneficiary beneficiary;
        private readonly Card card;
        private readonly ProductGroup productGroup;
        private readonly Subscription subscription1;
        private readonly Subscription subscription2;

        public GetRemovableFundTest()
        {
            project = new Project() { Name = "Project 1" };
            organization = new Organization() { Name = "Organization 1", Project = project };

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

            subscription1 = CreateSubscription("Subscription 1");
            subscription2 = CreateSubscription("Subscription 2");

            organization.Beneficiaries = new List<Beneficiary>() { beneficiary };
            beneficiary.Card = card;
            beneficiary.Subscriptions = new List<SubscriptionBeneficiary>();

            project.Subscriptions = new List<Subscription>() { subscription1, subscription2 };
            project.Organizations = new List<Organization>() { organization };
            project.Cards = new List<Card>() { card };

            DbContext.Cards.Add(card);
            DbContext.Beneficiaries.Add(beneficiary);
            DbContext.Organizations.Add(organization);
            DbContext.Projects.Add(project);
            DbContext.Subscriptions.Add(subscription1);
            DbContext.Subscriptions.Add(subscription2);

            foreach (var subscription in new[] { subscription1, subscription2 })
            {
                var budgetAllowance = new BudgetAllowance()
                {
                    AvailableFund = 100,
                    OriginalFund = 100,
                    Organization = organization,
                    Subscription = subscription
                };
                DbContext.BudgetAllowances.Add(budgetAllowance);
                beneficiary.Subscriptions.Add(new SubscriptionBeneficiary()
                {
                    Beneficiary = beneficiary,
                    Subscription = subscription,
                    BeneficiaryType = beneficiaryType,
                    BudgetAllowance = budgetAllowance
                });
            }

            DbContext.SaveChanges();

            handler = new GetRemovableFund(DbContext);
        }

        [Fact]
        public async Task ReturnsOnlyWhatTheSelectedSubscriptionPutOnTheCard()
        {
            // Le montage de CRCL-2659 : 36 $ sur le groupe de produits, dont 24 versés par le premier
            // abonnement et 12 par le second. Le montant retirable dépend de l'abonnement choisi.
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            for (var i = 0; i < 4; i++) AddSubscriptionFund(subscription1, 6, 6, today.AddDays(i));
            AddSubscriptionFund(subscription2, 12, 12, today.AddDays(10));
            DbContext.SaveChanges();

            (await Removable(subscription1)).Should().Be(24);
            (await Removable(subscription2)).Should().Be(12);
        }

        [Fact]
        public async Task IgnoresFundsAlreadySpent()
        {
            AddSubscriptionFund(subscription1, 18, 0, Clock.GetCurrentInstant().ToDateTimeUtc());
            AddSubscriptionFund(subscription1, 18, 5, Clock.GetCurrentInstant().ToDateTimeUtc());
            DbContext.SaveChanges();

            (await Removable(subscription1)).Should().Be(5);
        }

        [Fact]
        public async Task IgnoresExpiredFunds()
        {
            var expired = AddSubscriptionFund(subscription1, 10, 10, Clock.GetCurrentInstant().ToDateTimeUtc());
            expired.Status = FundTransactionStatus.Expired;
            DbContext.SaveChanges();

            (await Removable(subscription1)).Should().Be(0);
        }

        [Fact]
        public async Task ReturnsZeroWhenTheSubscriptionPutNothingOnTheCard()
        {
            AddSubscriptionFund(subscription1, 10, 10, Clock.GetCurrentInstant().ToDateTimeUtc());
            DbContext.SaveChanges();

            (await Removable(subscription2)).Should().Be(0);
        }

        private async Task<decimal> Removable(Subscription subscription)
        {
            return await handler.Handle(new GetRemovableFund.Query()
            {
                CardId = card.Id,
                SubscriptionId = subscription.Id,
                ProductGroupId = productGroup.Id
            }, CancellationToken.None);
        }

        private SubscriptionAddingFundTransaction AddSubscriptionFund(Subscription onSubscription, decimal amount, decimal availableFund, DateTime expirationDate)
        {
            var transaction = new SubscriptionAddingFundTransaction()
            {
                Amount = amount,
                AvailableFund = availableFund,
                Beneficiary = beneficiary,
                Card = card,
                CreatedAtUtc = Clock.GetCurrentInstant().ToDateTimeUtc(),
                ExpirationDate = expirationDate,
                Organization = organization,
                ProductGroup = productGroup,
                Status = FundTransactionStatus.Actived,
                SubscriptionType = onSubscription.Types.First()
            };

            card.Transactions.Add(transaction);
            DbContext.Transactions.Add(transaction);

            return transaction;
        }

        private Subscription CreateSubscription(string name)
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            return new Subscription()
            {
                Name = name,
                Project = project,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 12,
                        ProductGroup = productGroup
                    }
                },
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                FundsExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                IsFundsAccumulable = true
            };
        }
    }
}
