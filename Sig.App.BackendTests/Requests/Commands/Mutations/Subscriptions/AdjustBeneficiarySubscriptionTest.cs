using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Subscriptions
{
    public class AdjustBeneficiarySubscriptionTest : TestBase
    {
        private readonly AdjustBeneficiarySubscription handler;

        private readonly Project project;

        private readonly Organization organization;

        private readonly Subscription subscription1;
        private readonly Subscription subscription2;
        private readonly Subscription subscription3;

        private readonly Beneficiary beneficiary;

        private readonly Card card;

        private readonly BeneficiaryType beneficiaryType1;
        private readonly BeneficiaryType beneficiaryType2;

        private readonly ProductGroup productGroup1;
        private readonly ProductGroup productGroup2;

        private readonly BudgetAllowance budgetAllowance1;
        private readonly BudgetAllowance budgetAllowance2;
        private readonly BudgetAllowance budgetAllowance3;

        public AdjustBeneficiarySubscriptionTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            productGroup1 = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };

            productGroup2 = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_2,
                Name = "Product group 2",
                OrderOfAppearance = 2
            };

            DbContext.ProductGroups.Add(productGroup1);
            DbContext.ProductGroups.Add(productGroup2);

            organization = new Organization()
            {
                Name = "Organization 1",
                Project = project
            };
            DbContext.Organizations.Add(organization);

            beneficiaryType1 = new BeneficiaryType()
            {
                Project = project,
                Keys = "Beneficiary type 1",
                Name = "Beneficiary type 1"
            };

            beneficiaryType2 = new BeneficiaryType()
            {
                Project = project,
                Keys = "Beneficiary type 2",
                Name = "Beneficiary type 2"
            };

            DbContext.BeneficiaryTypes.Add(beneficiaryType1);
            DbContext.BeneficiaryTypes.Add(beneficiaryType2);

            beneficiary = new Beneficiary()
            {
                SortOrder = 0,
                Organization = organization,
                Firstname = "John1",
                Lastname = "Doe1",
                Address = "123, example street",
                Email = "john1.doe1@example.com",
                Phone = "555-555-1234",
                BeneficiaryType = beneficiaryType1
            };
            DbContext.Beneficiaries.Add(beneficiary);

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            subscription1 = new Subscription()
            {
                Name = "Subscription 1",
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 50,
                        BeneficiaryType = beneficiaryType1,
                        ProductGroup = productGroup1
                    },
                    new SubscriptionType()
                    {
                        Amount = 100,
                        BeneficiaryType = beneficiaryType2,
                        ProductGroup = productGroup1
                    },
                    new SubscriptionType()
                    {
                        Amount = 10,
                        BeneficiaryType = beneficiaryType2,
                        ProductGroup = productGroup2
                    }
                },
                Project = project
            };

            subscription2 = new Subscription()
            {
                Name = "Subscription 2",
                StartDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                EndDate = new DateTime(today.Year, today.Month, 16).AddMonths(2),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 25,
                        BeneficiaryType = beneficiaryType1
                    },
                    new SubscriptionType()
                    {
                        Amount = 50,
                        BeneficiaryType = beneficiaryType2
                    }
                },
                Project = project
            };

            subscription3 = new Subscription()
            {
                Name = "Subscription 3",
                StartDate = new DateTime(today.Year, today.Month, 1).AddMonths(1),
                EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(4),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 25,
                        BeneficiaryType = beneficiaryType1
                    },
                    new SubscriptionType()
                    {
                        Amount = 50,
                        BeneficiaryType = beneficiaryType2
                    }
                },
                Project = project,
                IsSubscriptionPaymentBasedCardUsage = true,
                MaxNumberOfPayments = 4
            };

            DbContext.Subscriptions.Add(subscription1);
            DbContext.Subscriptions.Add(subscription2);
            DbContext.Subscriptions.Add(subscription3);

            budgetAllowance1 = new BudgetAllowance()
            {
                AvailableFund = 600,
                Organization = organization,
                Subscription = subscription1,
                OriginalFund = 700
            };

            budgetAllowance2 = new BudgetAllowance()
            {
                AvailableFund = 625,
                Organization = organization,
                Subscription = subscription2,
                OriginalFund = 700
            };

            budgetAllowance3 = new BudgetAllowance()
            {
                AvailableFund = 600,
                Organization = organization,
                Subscription = subscription3,
                OriginalFund = 700
            };

            DbContext.BudgetAllowances.Add(budgetAllowance1);
            DbContext.BudgetAllowances.Add(budgetAllowance2);
            DbContext.BudgetAllowances.Add(budgetAllowance3);

            subscription1.Beneficiaries = new List<SubscriptionBeneficiary>() { new SubscriptionBeneficiary() { Beneficiary = beneficiary, Subscription = subscription1, BeneficiaryType = beneficiaryType1, BudgetAllowance = budgetAllowance1 } };
            subscription2.Beneficiaries = new List<SubscriptionBeneficiary>() { new SubscriptionBeneficiary() { Beneficiary = beneficiary, Subscription = subscription2, BeneficiaryType = beneficiaryType1, BudgetAllowance = budgetAllowance2 } };
            subscription3.Beneficiaries = new List<SubscriptionBeneficiary>() { new SubscriptionBeneficiary() { Beneficiary = beneficiary, Subscription = subscription3, BeneficiaryType = beneficiaryType1, BudgetAllowance = budgetAllowance3 } };

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
                        AvailableFund = 10,
                        Status = FundTransactionStatus.Actived,
                        ExpirationDate = new DateTime(today.Year + 1, today.Month, today.Day),
                        ProductGroup = productGroup1,
                        Beneficiary = beneficiary,
                        SubscriptionType = subscription1.Types.First(x => x.BeneficiaryType == beneficiaryType1)
                    }
                }
            };
            DbContext.Cards.Add(card);

            DbContext.SaveChanges();

            handler = new AdjustBeneficiarySubscription(NullLogger<AdjustBeneficiarySubscription>.Instance, Clock, DbContext);
        }

        [Fact]
        public async Task AdjustBeneficiarySubscription1()
        {
            beneficiary.BeneficiaryType = beneficiaryType2;
            DbContext.SaveChanges();

            var input = new AdjustBeneficiarySubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionIds = new List<Id>()
                {
                    subscription1.GetIdentifier()
                }
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiary = await DbContext.Beneficiaries
                .Include(x => x.Subscriptions).ThenInclude(x => x.BudgetAllowance)
                .Include(x => x.Subscriptions).ThenInclude(x => x.BeneficiaryType)
                .FirstAsync();

            var localSubscriptionBeneficiary = localBeneficiary.Subscriptions.First(x => x.SubscriptionId == subscription1.Id);
            localSubscriptionBeneficiary.BudgetAllowance.AvailableFund.Should().Be(540);
            localSubscriptionBeneficiary.BeneficiaryTypeId.Should().Be(beneficiaryType2.Id);
        }

        [Fact]
        public async Task AdjustBeneficiarySubscription2()
        {
            beneficiary.BeneficiaryType = beneficiaryType2;
            DbContext.SaveChanges();

            var input = new AdjustBeneficiarySubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionIds = new List<Id>()
                {
                    subscription2.GetIdentifier()
                }
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiary = await DbContext.Beneficiaries
                .Include(x => x.Subscriptions).ThenInclude(x => x.BudgetAllowance)
                .Include(x => x.Subscriptions).ThenInclude(x => x.BeneficiaryType)
                .FirstAsync();

            var localSubscriptionBeneficiary = localBeneficiary.Subscriptions.First(x => x.SubscriptionId == subscription2.Id);
            localSubscriptionBeneficiary.BudgetAllowance.AvailableFund.Should().Be(550);
            localSubscriptionBeneficiary.BeneficiaryTypeId.Should().Be(beneficiaryType2.Id);
        }

        [Fact]
        public async Task AdjustBeneficiarySubscription3()
        {
            beneficiary.BeneficiaryType = beneficiaryType2;
            DbContext.SaveChanges();

            var input = new AdjustBeneficiarySubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionIds = new List<Id>()
                {
                    subscription3.GetIdentifier()
                }
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiary = await DbContext.Beneficiaries
                .Include(x => x.Subscriptions).ThenInclude(x => x.BudgetAllowance)
                .Include(x => x.Subscriptions).ThenInclude(x => x.BeneficiaryType)
                .FirstAsync();

            var localSubscriptionBeneficiary = localBeneficiary.Subscriptions.First(x => x.SubscriptionId == subscription3.Id);
            localSubscriptionBeneficiary.BudgetAllowance.AvailableFund.Should().Be(500);
            localSubscriptionBeneficiary.BeneficiaryTypeId.Should().Be(beneficiaryType2.Id);
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotFound()
        {
            var input = new AdjustBeneficiarySubscription.Input()
            {
                BeneficiaryId = Id.New<Beneficiary>(123456),
                SubscriptionIds = new List<Id>()
                {
                    subscription1.GetIdentifier()
                }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AdjustBeneficiarySubscription.BeneficiaryNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionNotFound()
        {
            var input = new AdjustBeneficiarySubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionIds = new List<Id>()
                {
                    Id.New<Subscription>(123456)
                }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AdjustBeneficiarySubscription.SubscriptionNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfNotEnoughBudgetAllowance()
        {
            budgetAllowance1.AvailableFund = 0;
            DbContext.SaveChanges();

            var input = new AdjustBeneficiarySubscription.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionIds = new List<Id>()
                {
                    subscription1.GetIdentifier()
                }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AdjustBeneficiarySubscription.NotEnoughBudgetAllowanceException>();
        }
    }
}
