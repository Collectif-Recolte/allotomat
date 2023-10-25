using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.BackgroundJobs;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.BackgroundJobs
{
    public class DeactivateOffPlatformBeneficiaryTest : TestBase
    {
        private readonly Project project;
        private readonly Card card;
        private readonly OffPlatformBeneficiary beneficiary;
        private readonly Organization organization;
        private readonly DeactivateOffPlatformBeneficiary job;
        private readonly ProductGroup productGroup;

        public DeactivateOffPlatformBeneficiaryTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };

            organization = new Organization()
            {
                Name = "Organization 1",
                Project = project
            };

            productGroup = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };
            DbContext.ProductGroups.Add(productGroup);

            beneficiary = new OffPlatformBeneficiary()
            {
                ID1 = "1",
                Firstname = "John",
                Lastname = "Doe",
                Email = "john.doe@example.com",
                Phone = "555-555-1234",
                Address = "123, Example Street",
                OrganizationId = organization.Id,
                SortOrder = 0,
                IsActive = true,
                StartDate = DateTime.Today.AddDays(-20),
                EndDate = DateTime.Today.AddDays(-1),
                PaymentFunds = new List<PaymentFund>() {
                    new PaymentFund()
                    {
                        Amount = 10,
                        ProductGroup = productGroup
                    }
                },
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth
            };

            card = new Card()
            {
                Status = CardStatus.Assigned,
                Project = project,
                Beneficiary = beneficiary,
                Funds = new List<Fund>()
            };

            var fund = new Fund()
            {
                Amount = 20,
                ProductGroup = productGroup,
                Card = card
            };
            card.Funds.Add(fund);

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();

            organization.Beneficiaries = new List<Beneficiary>() { beneficiary };
            organization.Project = project;

            beneficiary.Organization = organization;
            beneficiary.Card = card;

            project.Organizations = new List<Organization>() { organization };
            project.Cards = new List<Card> { card };

            DbContext.Cards.Add(card);
            DbContext.Beneficiaries.Add(beneficiary);
            DbContext.Organizations.Add(organization);
            DbContext.Projects.Add(project);

            DbContext.SaveChanges();

            job = new DeactivateOffPlatformBeneficiary(DbContext, Clock, NullLogger<DeactivateOffPlatformBeneficiary>.Instance);
        }

        [Fact]
        public async Task DeactivateOffPlatformBeneficiary()
        {
            await job.Run();

            var localBeneficiary = DbContext.Beneficiaries.Select(x => x as OffPlatformBeneficiary).First();
            localBeneficiary.IsActive.Should().BeFalse();
            localBeneficiary.PaymentFunds.Count.Should().Be(0);
            localBeneficiary.Card.Funds.First().Amount.Should().Be(20);
        }

        [Fact]
        public async Task DontDeactivateActiveOffPlatformBeneficiary()
        {
            beneficiary.EndDate = DateTime.Today.AddDays(3);
            DbContext.SaveChanges();

            await job.Run();

            var localBeneficiary = DbContext.Beneficiaries.Select(x => x as OffPlatformBeneficiary).First();
            localBeneficiary.IsActive.Should().BeTrue();
        }
    }
}
