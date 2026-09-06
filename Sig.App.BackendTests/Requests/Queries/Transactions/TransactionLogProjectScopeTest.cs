using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql;
using Sig.App.Backend.Gql.Schema.Enums;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Requests.Commands.Queries.Transactions;
using Sig.App.Backend.Requests.Queries.Transactions;
using Sig.App.Backend.Services.Beneficiaries;
using Sig.App.Backend.Services.Permission;
using Sig.App.Backend.Services.Reports;
using Sig.App.Backend.Utilities;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Queries.Transactions
{
    /// <summary>
    /// ManageTransactions is a role-wide permission, so the guard on the field cannot tell whether the
    /// requested project belongs to the caller. These tests pin the data-side scoping that does.
    /// </summary>
    public class TransactionLogProjectScopeTest : TestBase
    {
        private static readonly DateTime PeriodStart = new DateTime(2025, 1, 1);
        private static readonly DateTime PeriodEnd = new DateTime(2025, 1, 31);

        private readonly Project ownProject;
        private readonly Project otherProject;
        private readonly AppUser projectManager;

        public TransactionLogProjectScopeTest()
        {
            ownProject = new Project { Name = "Programme du gestionnaire" };
            otherProject = new Project { Name = "Programme d'un autre client" };
            DbContext.Projects.AddRange(ownProject, otherProject);

            var ownOrganization = new Organization { Name = "Organisme 1", Project = ownProject };
            var otherOrganization = new Organization { Name = "Organisme 2", Project = otherProject };
            DbContext.Organizations.AddRange(ownOrganization, otherOrganization);
            DbContext.SaveChanges();

            AddTransactionLog(ownProject, ownOrganization, 10m);
            AddTransactionLog(otherProject, otherOrganization, 99m);
            DbContext.SaveChanges();

            projectManager = AddUser("manager@example.com", UserType.ProjectManager,
                claims: new[] { new Claim(AppClaimTypes.ProjectManagerOf, ownProject.Id.ToString()) });
            SetLoggedInUser(projectManager);
        }

        [Fact]
        public void NoClaimLeavesTheQueryUntouched()
        {
            DbContext.TransactionLogs.FilterByProjectScope(null).Should().HaveCount(2);
        }

        [Fact]
        public void TheClaimRestrictsTheQueryToThatProject()
        {
            var scoped = DbContext.TransactionLogs.FilterByProjectScope(ownProject.Id.ToString()).ToList();

            scoped.Should().ContainSingle();
            scoped[0].ProjectId.Should().Be(ownProject.Id);
        }

        [Fact]
        public void AnUnparsableClaimYieldsNothingRatherThanEverything()
        {
            DbContext.TransactionLogs.FilterByProjectScope("not-a-project-id").Should().BeEmpty();
        }

        [Fact]
        public async Task TheScreenRefusesToListAnotherProjectTransactions()
        {
            var result = await SearchHandler().Handle(SearchFor(otherProject), CancellationToken.None);

            result.Items.Should().BeEmpty();
        }

        [Fact]
        public async Task TheScreenStillListsTheManagerOwnProject()
        {
            var result = await SearchHandler().Handle(SearchFor(ownProject), CancellationToken.None);

            result.Items.Should().ContainSingle().Which.TotalAmount.Should().Be(10m);
        }

        [Fact]
        public async Task TheExportRefusesToReportAnotherProjectTransactions()
        {
            var stream = await ReportService().GenerateTransactionReport(ReportFor(otherProject));

            DataRowCount(stream).Should().Be(0);
        }

        [Fact]
        public async Task TheExportStillReportsTheManagerOwnProject()
        {
            var stream = await ReportService().GenerateTransactionReport(ReportFor(ownProject));

            DataRowCount(stream).Should().Be(1);
        }

        private static int DataRowCount(System.IO.Stream stream)
        {
            stream.Position = 0;
            ExcelReader.TryOpenStream(stream, out var reader).Should().BeTrue();

            return reader.Worksheet<ExportedRow>().String(x => x.FirstCell).GetData().Count();
        }

        private class ExportedRow
        {
            public string FirstCell { get; set; }
        }

        private SearchTransactionLogs SearchHandler()
        {
            return new SearchTransactionLogs(UserContext(), DbContext, UserManager, BeneficiaryService(),
                new PermissionService(DbContext));
        }

        private ReportService ReportService()
        {
            return new ReportService(UserContext(), DbContext, UserManager, BeneficiaryService(),
                new PermissionService(DbContext));
        }

        private IBeneficiaryService BeneficiaryService()
        {
            var mock = new Mock<IBeneficiaryService>();
            mock.Setup(x => x.CurrentUserCanSeeAllBeneficiaryInfo()).ReturnsAsync(true);
            return mock.Object;
        }

        private AppUserContext UserContext()
        {
            var services = new ServiceCollection();
            services.AddSingleton(HttpContextAccessor);
            return new AppUserContext(services.BuildServiceProvider(), null);
        }

        private static SearchTransactionLogs.Query SearchFor(Project project)
        {
            return new SearchTransactionLogs.Query
            {
                Page = new Page(1, 30),
                ProjectId = project.GetIdentifier(),
                StartDate = PeriodStart,
                EndDate = PeriodEnd
            };
        }

        private static GenerateTransactionsReport.Input ReportFor(Project project)
        {
            return new GenerateTransactionsReport.Input
            {
                ProjectId = project.GetIdentifier(),
                StartDate = PeriodStart,
                EndDate = PeriodEnd,
                TimeZoneId = "America/Montreal",
                Language = Language.French
            };
        }

        private void AddTransactionLog(Project project, Organization organization, decimal amount)
        {
            DbContext.TransactionLogs.Add(new TransactionLog
            {
                TransactionUniqueId = Guid.NewGuid().ToString(),
                CreatedAtUtc = PeriodStart.AddDays(1),
                Discriminator = TransactionLogDiscriminator.PaymentTransactionLog,
                TotalAmount = amount,
                ProjectId = project.Id,
                ProjectName = project.Name,
                OrganizationId = organization.Id,
                OrganizationName = organization.Name
            });
        }
    }
}
