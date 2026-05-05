using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
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
using static Sig.App.Backend.Requests.Commands.Mutations.Subscriptions.EditSubscription;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Subscriptions
{
    public class EditSubscriptionTest : TestBase
    {
        private readonly EditSubscription handler;
        private readonly Subscription subscription;
        private readonly SubscriptionType subscriptionType;
        private readonly Project project;
        private readonly ProductGroup productGroup;

        public EditSubscriptionTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            var organization = new Organization()
            {
                Name = "Organization 1",
                Project = project
            };
            DbContext.Organizations.Add(organization);

            var beneficiaryType = new BeneficiaryType()
            {
                Name = "Type 1",
                Project = project,
                Keys = "bliblou"
            };

            var beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Organization = organization,
                BeneficiaryType = beneficiaryType
            };
            DbContext.Beneficiaries.Add(beneficiary);

            subscriptionType = new SubscriptionType()
            {
                Amount = 25
            };

            subscription = new Subscription()
            {
                Name = "Subscription 1",
                StartDate = new DateTime(2022, 1, 1),
                EndDate = new DateTime(2022, 3, 30),
                FundsExpirationDate = new DateTime(2022, 4, 1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<SubscriptionType>()
                {
                    subscriptionType,
                    new SubscriptionType()
                    {
                        Amount = 50
                    },
                    new SubscriptionType()
                    {
                        Amount = 100
                    }
                },
                Project = project
            };
            DbContext.Subscriptions.Add(subscription);

            productGroup = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };
            DbContext.ProductGroups.Add(productGroup);

            DbContext.SaveChanges();

            handler = new EditSubscription(NullLogger<EditSubscription>.Instance, DbContext, Clock);
        }

        private Beneficiary AssignBeneficiaryToSubscription(BeneficiaryType beneficiaryType)
        {
            var beneficiary = new Beneficiary() { Firstname = "John", Lastname = "Doe", BeneficiaryType = beneficiaryType };
            DbContext.Beneficiaries.Add(beneficiary);
            subscription.Beneficiaries = new List<SubscriptionBeneficiary>()
            {
                new SubscriptionBeneficiary { Beneficiary = beneficiary, BeneficiaryType = beneficiaryType, Subscription = subscription }
            };
            DbContext.SaveChanges();
            return beneficiary;
        }

        [Fact]
        public async Task CanEditSubscription()
        {
            var localBeneficiaryType = await DbContext.BeneficiaryTypes.FirstAsync();
            var input = new Input()
            {
                SubscriptionId = subscription.GetIdentifier(),
                Name = "Subscription 1 test",
                StartDate = new LocalDate(2022, 2, 1),
                EndDate = new LocalDate(2022, 4, 30),
                FundsExpirationDate = new LocalDate(DateTime.UtcNow.Year + 1, 1, 1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                Types = new List<EditSubscriptionTypeInput>() { 
                    new EditSubscriptionTypeInput
                    {
                        BeneficiaryTypeId = localBeneficiaryType.GetIdentifier(),
                        Amount = 50,
                        ProductGroupId = productGroup.GetIdentifier()
                    }
                }
            };

            await handler.Handle(input, CancellationToken.None);

            var localSubscription = await DbContext.Subscriptions.FirstAsync();

            localSubscription.Name.Should().Be("Subscription 1 test");
            localSubscription.MonthlyPaymentMoment.Should().Be(SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth);
            localSubscription.StartDate.Should().Be(new DateTime(2022, 2, 1));
            localSubscription.EndDate.Should().Be(new DateTime(2022, 4, 30));
            localSubscription.FundsExpirationDate.Should().Be(new LocalDate(DateTime.UtcNow.Year + 1, 1, 1).AtMidnight().InUtc().ToDateTimeUtc());
        }

        [Fact]
        public async Task ThrowsIfSubscriptionNotFound()
        {
            var input = new Input()
            {
                SubscriptionId = Id.New<Subscription>(123456),
                Name = "Subscription 1 test",
                StartDate = new LocalDate(2022, 2, 1),
                EndDate = new LocalDate(2022, 4, 30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditSubscription.SubscriptionNotFoundException>();
        }

        [Fact]
        public async Task CanEditNameWithBeneficiaries()
        {
            var localBeneficiaryType = await DbContext.BeneficiaryTypes.FirstAsync();
            AssignBeneficiaryToSubscription(localBeneficiaryType);

            var input = new Input()
            {
                SubscriptionId = subscription.GetIdentifier(),
                Name = "Subscription renamed",
                StartDate = new LocalDate(2022, 1, 1),
                EndDate = new LocalDate(2022, 3, 30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Types = new List<EditSubscriptionTypeInput>() {
                    new EditSubscriptionTypeInput
                    {
                        BeneficiaryTypeId = localBeneficiaryType.GetIdentifier(),
                        Amount = 50,
                        ProductGroupId = productGroup.GetIdentifier()
                    }
                }
            };

            await handler.Handle(input, CancellationToken.None);

            var localSubscription = await DbContext.Subscriptions.FirstAsync();
            localSubscription.Name.Should().Be("Subscription renamed");
            localSubscription.MonthlyPaymentMoment.Should().Be(SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth);
        }

        [Fact]
        public async Task CanEditExpirationDateWithBeneficiaries()
        {
            var localBeneficiaryType = await DbContext.BeneficiaryTypes.FirstAsync();

            var futureExpiration = DateTime.UtcNow.AddYears(2);
            subscription.IsFundsAccumulable = true;
            subscription.TriggerFundExpiration = FundsExpirationTrigger.SpecificDate;
            subscription.FundsExpirationDate = futureExpiration;
            AssignBeneficiaryToSubscription(localBeneficiaryType);

            var newExpiration = new LocalDate(futureExpiration.Year + 1, 1, 1);
            var input = new Input()
            {
                SubscriptionId = subscription.GetIdentifier(),
                Name = "Subscription 1",
                StartDate = new LocalDate(2022, 1, 1),
                EndDate = new LocalDate(2022, 3, 30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                FundsExpirationDate = newExpiration,
                Types = new List<EditSubscriptionTypeInput>() {
                    new EditSubscriptionTypeInput
                    {
                        BeneficiaryTypeId = localBeneficiaryType.GetIdentifier(),
                        Amount = 50,
                        ProductGroupId = productGroup.GetIdentifier()
                    }
                }
            };

            await handler.Handle(input, CancellationToken.None);

            var localSubscription = await DbContext.Subscriptions.FirstAsync();
            localSubscription.FundsExpirationDate.Should().Be(newExpiration.AtMidnight().InUtc().ToDateTimeUtc());
        }

        [Fact]
        public async Task ThrowsIfExpirationDateEditedAfterExpiry()
        {
            var localBeneficiaryType = await DbContext.BeneficiaryTypes.FirstAsync();

            subscription.IsFundsAccumulable = true;
            subscription.TriggerFundExpiration = FundsExpirationTrigger.SpecificDate;
            subscription.FundsExpirationDate = DateTime.UtcNow.AddYears(-1);
            AssignBeneficiaryToSubscription(localBeneficiaryType);

            var input = new Input()
            {
                SubscriptionId = subscription.GetIdentifier(),
                Name = "Subscription 1",
                StartDate = new LocalDate(2022, 1, 1),
                EndDate = new LocalDate(2022, 3, 30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                FundsExpirationDate = new LocalDate(DateTime.UtcNow.Year + 1, 1, 1),
                Types = new List<EditSubscriptionTypeInput>() {
                    new EditSubscriptionTypeInput
                    {
                        BeneficiaryTypeId = localBeneficiaryType.GetIdentifier(),
                        Amount = 50,
                        ProductGroupId = productGroup.GetIdentifier()
                    }
                }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditSubscription.ExpirationDateAlreadyPassedException>();
        }

        [Fact]
        public async Task ThrowsIfExpirationDateEditedWhenNumberOfDaysTrigger()
        {
            var localBeneficiaryType = await DbContext.BeneficiaryTypes.FirstAsync();

            subscription.IsFundsAccumulable = true;
            subscription.TriggerFundExpiration = FundsExpirationTrigger.NumberOfDays;
            subscription.FundsExpirationDate = DateTime.UtcNow.AddYears(2);
            AssignBeneficiaryToSubscription(localBeneficiaryType);

            var input = new Input()
            {
                SubscriptionId = subscription.GetIdentifier(),
                Name = "Subscription 1",
                StartDate = new LocalDate(2022, 1, 1),
                EndDate = new LocalDate(2022, 3, 30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                FundsExpirationDate = new LocalDate(DateTime.UtcNow.Year + 1, 1, 1),
                Types = new List<EditSubscriptionTypeInput>() {
                    new EditSubscriptionTypeInput
                    {
                        BeneficiaryTypeId = localBeneficiaryType.GetIdentifier(),
                        Amount = 50,
                        ProductGroupId = productGroup.GetIdentifier()
                    }
                }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditSubscription.CantEditExpirationDateException>();
        }

        [Fact]
        public async Task ThrowsIfExpirationDateEditedWhenNotAccumulable()
        {
            var localBeneficiaryType = await DbContext.BeneficiaryTypes.FirstAsync();

            subscription.IsFundsAccumulable = false;
            subscription.TriggerFundExpiration = FundsExpirationTrigger.SpecificDate;
            subscription.FundsExpirationDate = DateTime.UtcNow.AddYears(2);
            AssignBeneficiaryToSubscription(localBeneficiaryType);

            var input = new Input()
            {
                SubscriptionId = subscription.GetIdentifier(),
                Name = "Subscription 1",
                StartDate = new LocalDate(2022, 1, 1),
                EndDate = new LocalDate(2022, 3, 30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                FundsExpirationDate = new LocalDate(DateTime.UtcNow.Year + 1, 1, 1),
                Types = new List<EditSubscriptionTypeInput>() {
                    new EditSubscriptionTypeInput
                    {
                        BeneficiaryTypeId = localBeneficiaryType.GetIdentifier(),
                        Amount = 50,
                        ProductGroupId = productGroup.GetIdentifier()
                    }
                }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditSubscription.CantEditExpirationDateException>();
        }

        [Fact]
        public async Task CanEditExpirationDateAlsoUpdatesExistingTransactions()
        {
            var localBeneficiaryType = await DbContext.BeneficiaryTypes.FirstAsync();

            var futureExpiration = DateTime.UtcNow.AddYears(2);
            subscription.IsFundsAccumulable = true;
            subscription.TriggerFundExpiration = FundsExpirationTrigger.SpecificDate;
            subscription.FundsExpirationDate = futureExpiration;
            var beneficiary = AssignBeneficiaryToSubscription(localBeneficiaryType);

            var card = new Card()
            {
                Funds = new List<Fund>(),
                Status = CardStatus.Assigned,
                Project = project,
                Beneficiary = beneficiary,
                Transactions = new List<Transaction>()
                {
                    new SubscriptionAddingFundTransaction()
                    {
                        Amount = 25,
                        AvailableFund = 25,
                        Status = FundTransactionStatus.Actived,
                        ExpirationDate = futureExpiration,
                        ProductGroup = productGroup,
                        Beneficiary = beneficiary,
                        SubscriptionType = subscriptionType
                    }
                }
            };
            DbContext.Cards.Add(card);
            DbContext.SaveChanges();

            var newExpiration = new LocalDate(futureExpiration.Year + 1, 1, 1);
            var input = new Input()
            {
                SubscriptionId = subscription.GetIdentifier(),
                Name = "Subscription 1",
                StartDate = new LocalDate(2022, 1, 1),
                EndDate = new LocalDate(2022, 3, 30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                FundsExpirationDate = newExpiration,
                Types = new List<EditSubscriptionTypeInput>() {
                    new EditSubscriptionTypeInput
                    {
                        BeneficiaryTypeId = localBeneficiaryType.GetIdentifier(),
                        Amount = 50,
                        ProductGroupId = productGroup.GetIdentifier()
                    }
                }
            };

            await handler.Handle(input, CancellationToken.None);

            var updatedTransaction = await DbContext.Transactions.OfType<SubscriptionAddingFundTransaction>().FirstAsync();
            updatedTransaction.ExpirationDate.Should().Be(newExpiration.AtMidnight().InUtc().ToDateTimeUtc());
        }

        [Fact]
        public async Task ThrowsIfMaxNumberOfPaymentsCantBeZero()
        {
            var localBeneficiaryType = await DbContext.BeneficiaryTypes.FirstAsync();
            var input = new Input()
            {
                SubscriptionId = subscription.GetIdentifier(),
                Name = "Subscription 1 test",
                StartDate = new LocalDate(2022, 2, 1),
                EndDate = new LocalDate(2022, 4, 30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                Types = new List<EditSubscriptionTypeInput>() {
                    new EditSubscriptionTypeInput
                    {
                        BeneficiaryTypeId = localBeneficiaryType.GetIdentifier(),
                        Amount = 50,
                        ProductGroupId = productGroup.GetIdentifier()
                    }
                },
                MaxNumberOfPayments = 0,
                IsSubscriptionPaymentBasedCardUsage = true
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditSubscription.MaxNumberOfPaymentsCantBeZeroException>();
        }

        [Fact]
        public async Task ThrowsIfNumberDaysUntilFundsExpireCantBe()
        {
            var localBeneficiaryType = await DbContext.BeneficiaryTypes.FirstAsync();
            var input = new Input()
            {
                SubscriptionId = subscription.GetIdentifier(),
                Name = "Subscription 1 test",
                StartDate = new LocalDate(2022, 2, 1),
                EndDate = new LocalDate(2022, 4, 30),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth,
                Types = new List<EditSubscriptionTypeInput>() {
                    new EditSubscriptionTypeInput
                    {
                        BeneficiaryTypeId = localBeneficiaryType.GetIdentifier(),
                        Amount = 50,
                        ProductGroupId = productGroup.GetIdentifier()
                    }
                },
                TriggerFundExpiration = FundsExpirationTrigger.NumberOfDays,
                NumberDaysUntilFundsExpire = 0
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditSubscription.NumberDaysUntilFundsExpireCantBeZeroException>();
        }
    }
}
