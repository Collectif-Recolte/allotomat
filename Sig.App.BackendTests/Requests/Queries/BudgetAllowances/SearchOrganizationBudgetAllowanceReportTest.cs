using FluentAssertions;
using Sig.App.Backend.DbModel.Entities.BudgetAllowanceLogs;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Requests.Queries.Organizations;
using Sig.App.Backend.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Queries.BudgetAllowances
{
    public class SearchOrganizationBudgetAllowanceReportTest : TestBase
    {
        private readonly SearchOrganizationBudgetAllowanceReport handler;

        private readonly Project project;
        private readonly Organization organization;
        private readonly Organization otherOrganization;
        private readonly Subscription subscription;
        private readonly Subscription otherSubscription;

        public SearchOrganizationBudgetAllowanceReportTest()
        {
            project = new Project() { Name = "Project 1" };
            organization = new Organization() { Name = "Organization 1", Project = project };
            otherOrganization = new Organization() { Name = "Organization 2", Project = project };
            subscription = new Subscription()
            {
                Name = "Subscription 1",
                Project = project,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31)
            };
            otherSubscription = new Subscription()
            {
                Name = "Subscription 2",
                Project = project,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31)
            };

            DbContext.Projects.Add(project);
            DbContext.Organizations.Add(organization);
            DbContext.Organizations.Add(otherOrganization);
            DbContext.Subscriptions.Add(subscription);
            DbContext.Subscriptions.Add(otherSubscription);
            DbContext.SaveChanges();

            handler = new SearchOrganizationBudgetAllowanceReport(DbContext);
        }

        [Fact]
        public async Task ReturnsTheAdjustmentsOfTheOrganizationEnvelopes()
        {
            AddLog(BudgetAllowanceLogDiscriminator.CreateBudgetAllowanceLog, 1000, organization, subscription);
            AddLog(BudgetAllowanceLogDiscriminator.EditBudgetAllowanceLog, 250, organization, subscription);

            var logs = await Report();

            logs.Should().HaveCount(2);
            logs.Should().OnlyContain(x => x.OrganizationId == organization.Id);
        }

        [Fact]
        public async Task IgnoresTheAdjustmentsOfTheOtherOrganizations()
        {
            AddLog(BudgetAllowanceLogDiscriminator.CreateBudgetAllowanceLog, 1000, organization, subscription);
            AddLog(BudgetAllowanceLogDiscriminator.CreateBudgetAllowanceLog, 2000, otherOrganization, otherSubscription);

            var logs = await Report();

            logs.Should().ContainSingle().Which.Amount.Should().Be(1000);
        }

        [Fact]
        public async Task ReturnsTheTransfersReceivedByTheOrganization()
        {
            // Un transfert entrant augmente l'enveloppe du groupe sans que celui-ci soit la source du
            // journal : sans cette ligne, le total du groupe changerait sans explication.
            AddLog(BudgetAllowanceLogDiscriminator.MoveBudgetAllowanceLog, 300, otherOrganization, otherSubscription,
                target: organization, targetSubscription: subscription);

            var logs = await Report();

            logs.Should().ContainSingle().Which.TargetOrganizationId.Should().Be(organization.Id);
        }

        [Fact]
        public async Task IgnoresTheAdjustmentsOutsideTheRequestedPeriod()
        {
            AddLog(BudgetAllowanceLogDiscriminator.CreateBudgetAllowanceLog, 1000, organization, subscription,
                createdAt: new DateTime(2025, 6, 1));

            var logs = await Report();

            logs.Should().BeEmpty();
        }

        [Fact]
        public async Task KeepsOnlyTheRequestedSubscriptions()
        {
            AddLog(BudgetAllowanceLogDiscriminator.CreateBudgetAllowanceLog, 1000, organization, subscription);
            AddLog(BudgetAllowanceLogDiscriminator.CreateBudgetAllowanceLog, 500, organization, otherSubscription);

            var logs = await Report(new[] { subscription.Id });

            logs.Should().ContainSingle().Which.Amount.Should().Be(1000);
        }

        private async Task<List<BudgetAllowanceLog>> Report(IEnumerable<long> subscriptions = null)
        {
            var result = await handler.Handle(new SearchOrganizationBudgetAllowanceReport.Query()
            {
                OrganizationId = organization.Id,
                Page = new Page(1, 30),
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 12, 31),
                Subscriptions = subscriptions
            }, CancellationToken.None);

            return result.Items.ToList();
        }

        private void AddLog(BudgetAllowanceLogDiscriminator discriminator, decimal amount, Organization source,
            Subscription sourceSubscription, Organization target = null, Subscription targetSubscription = null,
            DateTime? createdAt = null)
        {
            DbContext.BudgetAllowanceLogs.Add(new BudgetAllowanceLog()
            {
                CreatedAtUtc = createdAt ?? new DateTime(2026, 6, 1),
                Discriminator = discriminator,
                ProjectId = project.Id,
                Amount = amount,
                OrganizationId = source.Id,
                OrganizationName = source.Name,
                SubscriptionId = sourceSubscription.Id,
                SubscriptionName = sourceSubscription.Name,
                TargetOrganizationId = target?.Id,
                TargetOrganizationName = target?.Name,
                TargetSubscriptionId = targetSubscription?.Id,
                TargetSubscriptionName = targetSubscription?.Name
            });
            DbContext.SaveChanges();
        }
    }
}
