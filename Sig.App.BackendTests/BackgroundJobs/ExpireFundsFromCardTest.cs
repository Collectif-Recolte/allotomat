using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.BackgroundJobs;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.BackgroundJobs
{
    public class ExpireFundsFromCardTest : TestBase
    {
        private readonly Project project;
        private readonly Card card;
        private readonly Beneficiary beneficiary;
        private readonly Organization organization;
        private readonly ExpireFundsFromCard job;
        private readonly ProductGroup productGroup;

        public ExpireFundsFromCardTest()
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

            beneficiary = new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Organization = organization
            };

            productGroup = new ProductGroup()
            {
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1,
                Project = project
            };
            DbContext.ProductGroups.Add(productGroup);

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
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
                        AvailableFund = 0,
                        Status = FundTransactionStatus.Expired,
                        ExpirationDate = new DateTime(today.Year - 1, today.Month, today.Day),
                        ProductGroup = productGroup,
                        Beneficiary = beneficiary
                    },
                    new ManuallyAddingFundTransaction()
                    {
                        Amount = 30,
                        AvailableFund = 0,
                        Status = FundTransactionStatus.Actived,
                        ExpirationDate = new DateTime(today.Year - 1, today.Month, today.Day),
                        ProductGroup = productGroup,
                        Beneficiary = beneficiary
                    },
                    new SubscriptionAddingFundTransaction()
                    {
                        Amount = 20,
                        AvailableFund = 10,
                        Status = FundTransactionStatus.Actived,
                        ExpirationDate = new DateTime(today.Year - 1, today.Month, today.Day),
                        ProductGroup = productGroup,
                        Beneficiary = beneficiary
                    },
                    new ManuallyAddingFundTransaction()
                    {
                        Amount = 20,
                        AvailableFund = 20,
                        Status = FundTransactionStatus.Actived,
                        ExpirationDate = new DateTime(today.Year + 1, today.Month, today.Day),
                        ProductGroup = productGroup,
                        Beneficiary = beneficiary
                    }
                }
            };

            card.Funds.Add(new Fund()
            {
                Amount = 30,
                Card = card,
                ProductGroup = productGroup
            });

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

            job = new ExpireFundsFromCard(DbContext, Clock, NullLogger<ExpireFundsFromCard>.Instance);
        }

        [Fact]
        public async Task ExpireFundsFromCard()
        {
            await job.Run();

            var card = DbContext.Cards.Include(x => x.Funds).First();
            card.Funds.First().Amount.Should().Be(20);
            card.Transactions.Where(x => x.GetType() == typeof(SubscriptionAddingFundTransaction) && (x as SubscriptionAddingFundTransaction).Status == FundTransactionStatus.Expired).Should().HaveCount(2);
            card.Transactions.Where(x => x.GetType() == typeof(ManuallyAddingFundTransaction) && (x as ManuallyAddingFundTransaction).Status == FundTransactionStatus.Expired).Should().HaveCount(1);

            var transactionLogCreated = await DbContext.TransactionLogs.Where(x => x.Discriminator == TransactionLogDiscriminator.ExpireFundTransactionLog).ToListAsync();
            transactionLogCreated.Count.Should().Be(2);
            transactionLogCreated.Any(x => x.TotalAmount == 10).Should().BeTrue();
            transactionLogCreated.Any(x => x.TotalAmount == 0).Should().BeTrue();
        }
    }
}
