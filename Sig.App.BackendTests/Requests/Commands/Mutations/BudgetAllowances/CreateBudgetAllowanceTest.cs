using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Plugins.BudgetAllowances;
using Sig.App.Backend.Requests.Commands.Mutations.BudgetAllowances;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.BudgetAllowances
{
    public class CreateBudgetAllowanceTest : TestBase
    {
        private readonly CreateBudgetAllowance handler;
        private readonly Organization organization;
        private readonly Subscription subscription;

        public CreateBudgetAllowanceTest()
        {
            var user = AddUser("test@example.com", UserType.ProjectManager, password: "Abcd1234!!");
            SetLoggedInUser(user);

            user.Profile = new UserProfile()
            {
                FirstName = "Test",
                LastName = "Example",
                User = user,
                UpdateTimeUtc = DateTime.UtcNow
            };

            var budgetAllowanceLogFactory = new BudgetAllowanceLogFactory(Clock, HttpContextAccessor, DbContext);

            var project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            organization = new Organization()
            {
                Name = "Organization 1",
                Project = project
            };
            DbContext.Organizations.Add(organization);

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            subscription = new Subscription()
            {
                Name = "Subscription 1",
                Project = project,
                Types = new List<SubscriptionType>()
                {
                    new SubscriptionType()
                    {
                        Amount = 25
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
                StartDate = new DateTime(today.Year, today.Month, 1)
            };
            DbContext.Subscriptions.Add(subscription);

            DbContext.SaveChanges();

            handler = new CreateBudgetAllowance(NullLogger<CreateBudgetAllowance>.Instance, DbContext, budgetAllowanceLogFactory);
        }

        [Fact]
        public async Task AddBudgetAllowanceToOrganizationAndSubscription()
        {
            var input = new CreateBudgetAllowance.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                Amount = 25
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = await DbContext.BudgetAllowances.FirstAsync();
            var localSubscription = await DbContext.Subscriptions.FirstAsync();
            var localOrganization = await DbContext.Organizations.FirstAsync();

            localSubscription.BudgetAllowances.Should().HaveCount(1);
            localOrganization.BudgetAllowances.Should().HaveCount(1);

            localBudgetAllowance.Subscription.Should().Be(subscription);
            localBudgetAllowance.Organization.Should().Be(organization);
            localBudgetAllowance.AvailableFund.Should().Be(25);
            localBudgetAllowance.OriginalFund.Should().Be(25);
        }

        [Fact]
        public async Task ThrowsIfOrganizationNotFound()
        {
            var input = new CreateBudgetAllowance.Input()
            {
                OrganizationId = Id.New<Organization>(123456),
                SubscriptionId = subscription.GetIdentifier(),
                Amount = 25
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateBudgetAllowance.OrganizationNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionNotFound()
        {
            var input = new CreateBudgetAllowance.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = Id.New<Subscription>(123456),
                Amount = 25
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateBudgetAllowance.SubscriptionNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfSubscriptionAndOrganizationNotRelated()
        {
            var project = new Project()
            {
                Name = "Project 2"
            };
            DbContext.Projects.Add(project);

            var organization2 = new Organization()
            {
                Name = "Organization 2",
                Project = project
            };
            DbContext.Organizations.Add(organization2);
            DbContext.SaveChanges();

            var input = new CreateBudgetAllowance.Input()
            {
                OrganizationId = organization2.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                Amount = 25
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateBudgetAllowance.OrganizationAndSubscriptionNotRelated>();
        }

        [Fact]
        public async Task ThrowsIfOrganizationAlreadyHaveBudgetForSubscription()
        {
            var budgetAllowance = new BudgetAllowance()
            {
                Organization = organization,
                Subscription = subscription,
                OriginalFund = 25,
                AvailableFund = 25
            };

            DbContext.BudgetAllowances.Add(budgetAllowance);
            DbContext.SaveChanges();

            var input = new CreateBudgetAllowance.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier(),
                Amount = 25
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateBudgetAllowance.OrganizationAlreadyHaveBudgetForSubscriptionException>();
        }
    }
}
