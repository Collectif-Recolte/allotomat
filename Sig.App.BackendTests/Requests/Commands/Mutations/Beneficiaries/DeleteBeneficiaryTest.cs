using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries;
using Sig.App.Backend.Requests.Commands.Mutations.Cards;
using Sig.App.Backend.Requests.Commands.Mutations.Markets;
using Sig.App.Backend.Requests.Commands.Mutations.Subscriptions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Beneficiaries
{
    public class DeleteBeneficiaryTest : TestBase
    {
        private readonly IRequestHandler<DeleteBeneficiary.Input> handler;
        private readonly Beneficiary beneficiary;
        private readonly BeneficiaryType beneficiaryType;
        private readonly Project project;
        private readonly Organization organization;

        public DeleteBeneficiaryTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };

            organization = new Organization()
            {
                Name = "Organization1",
                Project = project
            };

            beneficiaryType = new BeneficiaryType()
            {
                Name = "Type 1",
                Keys = "type1",
                Project = project
            };

            DbContext.Projects.Add(project);
            DbContext.Organizations.Add(organization);
            DbContext.BeneficiaryTypes.Add(beneficiaryType);
            DbContext.SaveChanges();

            beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Address = "123, Example Street",
                Organization = organization,
                BeneficiaryTypeId = beneficiaryType.Id
            };

            DbContext.Beneficiaries.Add(beneficiary);
            DbContext.SaveChanges();

            handler = new DeleteBeneficiary(NullLogger<DeleteBeneficiary>.Instance, DbContext, Clock);
        }

        [Fact]
        public async Task DeleteTheBeneficiary()
        {
            var input = new DeleteBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var beneficiarycount = await DbContext.Beneficiaries.CountAsync();
            beneficiarycount.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryNotFound()
        {
            var input = new DeleteBeneficiary.Input()
            {
                BeneficiaryId = Id.New<Beneficiary>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteBeneficiary.BeneficiaryNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryHaveActiveSubscription()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            var subscriptionType = new SubscriptionType()
            {
                Amount = 25,
                BeneficiaryType = beneficiaryType
            };

            var subscription = new Subscription()
            {
                Name = "Subscription 1",
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 1).AddMonths(2).AddDays(-1),
                FundsExpirationDate = new DateTime(today.Year, today.Month, 1).AddMonths(3).AddDays(-1),
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

            var budgetAllowance = new BudgetAllowance()
            {
                AvailableFund = 25,
                Organization = organization,
                Subscription = subscription,
                OriginalFund = 75
            };
            DbContext.BudgetAllowances.Add(budgetAllowance);

            subscription.Beneficiaries = new List<SubscriptionBeneficiary>() { new SubscriptionBeneficiary { Beneficiary = beneficiary, BeneficiaryType = beneficiary.BeneficiaryType, Subscription = subscription, BudgetAllowance = budgetAllowance } };

            DbContext.Subscriptions.Add(subscription);

            DbContext.SaveChanges();

            var input = new DeleteBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteBeneficiary.BeneficiaryCantHaveActiveSubscriptionException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryHaveCard()
        {
            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            var card = new Card()
            {
                Status = CardStatus.Unassigned,
                Project = project,
                ProgramCardId = 1
            };
            DbContext.Cards.Add(card);

            await DbContext.SaveChangesAsync();

            var inputAssign = new AssignCardToBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                CardId = card.ProgramCardId
            };
            await new AssignCardToBeneficiary(NullLogger<AssignCardToBeneficiary>.Instance, DbContext).Handle(inputAssign, CancellationToken.None);

            var input = new DeleteBeneficiary.Input()
            {
                BeneficiaryId = beneficiary.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteBeneficiary.BeneficiaryCantHaveCardException>();
        }
    }
}
