using FluentAssertions;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Requests.Queries.Organizations;
using Sig.App.Backend.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Queries.Organizations
{
    public class SearchOrganizationSubscriptionEndReportTest : TestBase
    {
        private const long DeletedSubscriptionId = 999;

        private readonly SearchOrganizationSubscriptionEndReport handler;

        private readonly Project project;
        private readonly Organization organization;
        private readonly Subscription subscription;

        public SearchOrganizationSubscriptionEndReportTest()
        {
            project = new Project() { Name = "Project 1" };
            organization = new Organization() { Name = "Organization 1", Project = project };
            subscription = new Subscription()
            {
                Name = "Subscription 1",
                Project = project,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31)
            };

            DbContext.Projects.Add(project);
            DbContext.Organizations.Add(organization);
            DbContext.Subscriptions.Add(subscription);
            DbContext.SaveChanges();

            handler = new SearchOrganizationSubscriptionEndReport(DbContext);
        }

        [Fact]
        public async Task GroupsTransactionsBySubscription()
        {
            AddPayment(subscription.Id, 10);
            AddPayment(subscription.Id, 15);

            var transactions = await SubscriptionEndTransactions();

            transactions.Should().HaveCount(1);
            transactions.Single().Subscription.Should().NotBeNull();
            transactions.Single().Subscription.Name.Value.Should().Be("Subscription 1");
            transactions.Single().TotalPurchaseValue.Should().Be(25);
        }

        [Fact]
        public async Task ReturnsNoSubscriptionWhenItWasDeletedSinceTheTransaction()
        {
            // CRCL-2684 : les journaux de transaction gardent le SubscriptionId même après la suppression
            // de l'abonnement. Le rapport doit rester consultable, sans abonnement pour ces lignes.
            AddPayment(subscription.Id, 10);
            AddPayment(DeletedSubscriptionId, 40);

            var transactions = await SubscriptionEndTransactions();

            transactions.Should().HaveCount(2);
            transactions.Should().ContainSingle(x => x.Subscription == null && x.TotalPurchaseValue == 40);
            transactions.Should().ContainSingle(x => x.Subscription != null && x.TotalPurchaseValue == 10);
        }

        private async Task<List<SubscriptionEndTransactionGraphType>> SubscriptionEndTransactions()
        {
            var result = await handler.Handle(new SearchOrganizationSubscriptionEndReport.Query()
            {
                OrganizationId = organization.Id,
                Page = new Page(1, 30),
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31)
            }, CancellationToken.None);

            var item = result.Items.Should().ContainSingle().Subject;
            return item.SubscriptionEndTransactions.ToList();
        }

        private void AddPayment(long subscriptionId, decimal amount)
        {
            DbContext.TransactionLogs.Add(new TransactionLog()
            {
                TransactionUniqueId = Guid.NewGuid().ToString(),
                CreatedAtUtc = new DateTime(2026, 6, 1),
                Discriminator = TransactionLogDiscriminator.PaymentTransactionLog,
                TotalAmount = amount,
                OrganizationId = organization.Id,
                OrganizationName = organization.Name,
                ProjectId = project.Id,
                ProjectName = project.Name,
                SubscriptionId = subscriptionId,
                SubscriptionName = "Whatever the subscription was called"
            });
            DbContext.SaveChanges();
        }
    }
}
