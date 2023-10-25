using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Subscriptions;
using System;
using System.Collections.Generic;
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

            handler = new EditSubscription(NullLogger<EditSubscription>.Instance, DbContext);
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
                FundsExpirationDate = new LocalDate(2022, 5, 1),
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
            localSubscription.FundsExpirationDate.Should().Be(new DateTime(2022, 5, 1));
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
        public async Task ThrowsIfSubscriptionCantEdit()
        {
            var localBeneficiaryType = await DbContext.BeneficiaryTypes.FirstAsync(); ;

            var beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Address = "123, example street",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                BeneficiaryType = localBeneficiaryType
            };
            DbContext.Beneficiaries.Add(beneficiary);

            subscription.Beneficiaries = new List<SubscriptionBeneficiary>() { new SubscriptionBeneficiary { Beneficiary = beneficiary, BeneficiaryType = localBeneficiaryType, Subscription = subscription } };
            DbContext.SaveChanges();

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
                }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditSubscription.CantEditSubscriptionWithBeneficiaries>();
        }
    }
}
