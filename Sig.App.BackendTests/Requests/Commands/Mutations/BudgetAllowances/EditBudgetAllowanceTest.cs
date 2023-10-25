using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.BudgetAllowances;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.BudgetAllowances
{
    public class EditBudgetAllowanceTest : TestBase
    {
        private readonly EditBudgetAllowance handler;
        private readonly BudgetAllowance budgetAllowance;

        public EditBudgetAllowanceTest()
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

            budgetAllowance = new BudgetAllowance()
            {
                AvailableFund = 20,
                Organization = organization,
                Subscription = subscription,
                OriginalFund = 25
            };
            DbContext.BudgetAllowances.Add(budgetAllowance);

            DbContext.SaveChanges();

            handler = new EditBudgetAllowance(NullLogger<EditBudgetAllowance>.Instance, DbContext);
        }

        [Fact]
        public async Task EditBudgetAllowance()
        {
            var input = new EditBudgetAllowance.Input()
            {
                BudgetAllowanceId = budgetAllowance.GetIdentifier(),
                Amount = 10
            };

            await handler.Handle(input, CancellationToken.None);

            var localBudgetAllowance = await DbContext.BudgetAllowances.FirstAsync();
            
            localBudgetAllowance.AvailableFund.Should().Be(5);
            localBudgetAllowance.OriginalFund.Should().Be(10);
        }

        [Fact]
        public async Task ThrowsIfBudgetAllowanceNotFound()
        {
            var input = new EditBudgetAllowance.Input()
            {
                BudgetAllowanceId = Id.New<BudgetAllowance>(123456),
                Amount = 25
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditBudgetAllowance.BudgetAllowanceNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfAvailableBudgetOverNewBudget()
        {
            var input = new EditBudgetAllowance.Input()
            {
                BudgetAllowanceId = budgetAllowance.GetIdentifier(),
                Amount = 4
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditBudgetAllowance.AvailableBudgetOverNewBudgetException>();
        }
    }
}
