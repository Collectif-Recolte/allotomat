using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
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
    public class DeleteBudgetAllowanceTest : TestBase
    {
        private readonly IRequestHandler<DeleteBudgetAllowance.Input> handler;
        private readonly Project project;
        private readonly BudgetAllowance budgetAllowance;
        private readonly Subscription subscription;

        public DeleteBudgetAllowanceTest()
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

            budgetAllowance = new BudgetAllowance()
            {
                AvailableFund = 20,
                Organization = organization,
                Subscription = subscription,
                OriginalFund = 25
            };
            DbContext.BudgetAllowances.Add(budgetAllowance);

            DbContext.SaveChanges();

            handler = new DeleteBudgetAllowance(NullLogger<DeleteBudgetAllowance>.Instance, DbContext, budgetAllowanceLogFactory);
        }

        [Fact]
        public async Task DeleteBudgetAllowance()
        {
            var input = new DeleteBudgetAllowance.Input()
            {
                BudgetAllowanceId = budgetAllowance.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var beneficiarycount = await DbContext.BudgetAllowances.CountAsync();
            beneficiarycount.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfBudgetAllowanceNotFound()
        {
            var input = new DeleteBudgetAllowance.Input()
            {
                BudgetAllowanceId = Id.New<BudgetAllowance>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteBudgetAllowance.BudgetAllowanceNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBudgetAllowanceHaveBeneficiaries()
        {
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
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Address = "123, Example Street",
                BeneficiaryType = beneficiaryType
            };
            DbContext.Beneficiaries.Add(beneficiary);

            var SubscriptionBeneficiary = new SubscriptionBeneficiary()
            {
                Beneficiary = beneficiary,
                BeneficiaryType = beneficiary.BeneficiaryType,
                Subscription = subscription,
                BudgetAllowance = budgetAllowance
            };
            DbContext.SubscriptionBeneficiaries.Add(SubscriptionBeneficiary);

            DbContext.SaveChanges();

            var input = new DeleteBudgetAllowance.Input()
            {
                BudgetAllowanceId = budgetAllowance.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteBudgetAllowance.BudgetAllowanceCantHaveBeneficiariesException>();
        }
    }
}
