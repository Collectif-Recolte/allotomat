using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Markets;
using Sig.App.Backend.Requests.Commands.Mutations.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Subscriptions
{
    public class DeleteSubscriptionTest : TestBase
    {
        private readonly IRequestHandler<DeleteSubscription.Input> handler;
        private readonly Subscription subscription;
        private readonly SubscriptionType subscriptionType;

        public DeleteSubscriptionTest()
        {
            var project = new Project()
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

            subscriptionType = new SubscriptionType()
            {
                Amount = 25
            };

            var beneficiaryType = new BeneficiaryType()
            {
                Keys = "Type1",
                Name = "Type1",
                Project = project
            };
            DbContext.BeneficiaryTypes.Add(beneficiaryType);

            var beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Address = "123, example street",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                BeneficiaryType = beneficiaryType
            };
            DbContext.Beneficiaries.Add(beneficiary);

            subscription = new Subscription()
            {
                Name = "Subscription 1",
                StartDate = new DateTime(2022, 1, 1),
                EndDate = new DateTime(2022, 3, 30),
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

            subscription.Beneficiaries = new List<SubscriptionBeneficiary>() { new SubscriptionBeneficiary { Beneficiary = beneficiary, BeneficiaryType = beneficiary.BeneficiaryType, Subscription = subscription } };

            DbContext.Subscriptions.Add(subscription);

            DbContext.SaveChanges();

            handler = new DeleteSubscription(NullLogger<DeleteSubscription>.Instance, DbContext);
        }

        [Fact]
        public async Task CanDeleteSubscription()
        {
            DbContext.SubscriptionBeneficiaries.Remove(subscription.Beneficiaries.First());
            DbContext.SaveChanges();

            var input = new DeleteSubscription.Input()
            {
                SubscriptionId = subscription.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localSubscription = await DbContext.Subscriptions.ToListAsync();
            var localSubscriptionTypes = await DbContext.SubscriptionTypes.ToListAsync();
            var localSubscriptionBeneficiaries = await DbContext.SubscriptionBeneficiaries.ToListAsync();

            localSubscription.Count.Should().Be(0);
            localSubscriptionTypes.Count.Should().Be(0);
            localSubscriptionBeneficiaries.Count.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfSubsriptionNotFound()
        {
            var input = new DeleteSubscription.Input()
            {
                SubscriptionId = Id.New<Subscription>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteSubscription.SubscriptionNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfSubsriptionHaveBeneficiaries()
        {
            var input = new DeleteSubscription.Input()
            {
                SubscriptionId = subscription.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteSubscription.CantDeleteSubscriptionWithBeneficiaries>();
        }
    }
}
