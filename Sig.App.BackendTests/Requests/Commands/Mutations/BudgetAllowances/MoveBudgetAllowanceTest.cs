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
    public class MoveBudgetAllowanceTest : TestBase
    {
        private readonly MoveBudgetAllowance handler;
        private readonly BudgetAllowance initialBudgetAllowance;
        private readonly BudgetAllowance targetBudgetAllowance;

        public MoveBudgetAllowanceTest()
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

            var organization = new Organization()
            {
                Name = "Organization 1",
                Project = project
            };
            DbContext.Organizations.Add(organization);

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            var subscription = new Subscription()
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

            initialBudgetAllowance = new BudgetAllowance()
            {
                AvailableFund = 20,
                Organization = organization,
                Subscription = subscription,
                OriginalFund = 25
            };
            DbContext.BudgetAllowances.Add(initialBudgetAllowance);

            targetBudgetAllowance = new BudgetAllowance()
            {
                AvailableFund = 20,
                Organization = organization,
                Subscription = subscription,
                OriginalFund = 25
            };
            DbContext.BudgetAllowances.Add(targetBudgetAllowance);

            DbContext.SaveChanges();

            handler = new MoveBudgetAllowance(NullLogger<MoveBudgetAllowance>.Instance, DbContext, budgetAllowanceLogFactory);
        }

        [Fact]
        public async Task MoveBudgetAllowance()
        {
            var input = new MoveBudgetAllowance.Input()
            {
                InitialBudgetAllowanceId = initialBudgetAllowance.GetIdentifier(),
                TargetBudgetAllowanceId = targetBudgetAllowance.GetIdentifier(),
                Amount = 10
            };

            await handler.Handle(input, CancellationToken.None);

            var localInitialBudgetAllowance = await DbContext.BudgetAllowances.FirstAsync(x => x.Id == initialBudgetAllowance.Id);
            var localTargetBudgetAllowance = await DbContext.BudgetAllowances.FirstAsync(x => x.Id == targetBudgetAllowance.Id);

            localInitialBudgetAllowance.AvailableFund.Should().Be(10);
            localInitialBudgetAllowance.OriginalFund.Should().Be(15);

            localTargetBudgetAllowance.AvailableFund.Should().Be(30);
            localTargetBudgetAllowance.OriginalFund.Should().Be(35);
        }

        [Fact]
        public async Task ThrowsIfInitialBudgetAllowanceNotFound()
        {
            var input = new MoveBudgetAllowance.Input()
            {
                InitialBudgetAllowanceId = Id.New<BudgetAllowance>(123456),
                TargetBudgetAllowanceId = targetBudgetAllowance.GetIdentifier(),
                Amount = 25
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<MoveBudgetAllowance.InitialBudgetAllowanceNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfTargetBudgetAllowanceNotFound()
        {
            var input = new MoveBudgetAllowance.Input()
            {
                InitialBudgetAllowanceId = initialBudgetAllowance.GetIdentifier(),
                TargetBudgetAllowanceId = Id.New<BudgetAllowance>(123456),
                Amount = 25
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<MoveBudgetAllowance.TargetBudgetAllowanceNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfAvailableBudgetOverNewBudget()
        {
            var input = new MoveBudgetAllowance.Input()
            {
                InitialBudgetAllowanceId = initialBudgetAllowance.GetIdentifier(),
                TargetBudgetAllowanceId = targetBudgetAllowance.GetIdentifier(),
                Amount = 40
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<MoveBudgetAllowance.AvailableBudgetUnderRequestAmountException>();
        }
    }
}
