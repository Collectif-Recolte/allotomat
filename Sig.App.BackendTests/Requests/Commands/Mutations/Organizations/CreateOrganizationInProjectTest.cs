using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Organizations;
using Sig.App.Backend.Services.Mailer;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Organizations
{
    public class CreateOrganizationInProjectTest : TestBase
    {
        private readonly CreateOrganizationInProject handler;
        private readonly Project project;
        private Mock<IMailer> mailer;

        public CreateOrganizationInProjectTest()
        {
            mailer = new Mock<IMailer>();

            project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            DbContext.SaveChanges();

            handler = new CreateOrganizationInProject(NullLogger<CreateOrganizationInProject>.Instance, DbContext, UserManager, mailer.Object);
        }

        [Fact]
        public async Task CreateTheOrganization()
        {
            var input = new CreateOrganizationInProject.Input()
            {
                Name = "Organization Test 1",
                ManagerEmails = new string[1] { "test1@example.com" },
                ProjectId = project.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var organization = await DbContext.Organizations.FirstAsync();

            organization.Name.Should().Be("Organization Test 1");
        }

        [Fact]
        public async Task SendsConfirmationEmail()
        {
            var input = new CreateOrganizationInProject.Input()
            {
                Name = "Organization Test 1",
                ManagerEmails = new string[1] { "test1@example.com" },
                ProjectId = project.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            mailer.Verify(x => x.Send(It.IsAny<OrganizationManagerInviteEmail>()));
        }

        [Fact]
        public async Task ThrowsIfProjectNotFound()
        {
            var input = new CreateOrganizationInProject.Input()
            {
                Name = "Organization Test 1",
                ManagerEmails = new string[1] { "test1@example.com" },
                ProjectId = Id.New<Project>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateOrganizationInProject.ProjectNotFoundException>();
        }

        [Fact]
        public async Task AssignsManagerClaimWithRealOrganizationId()
        {
            var input = new CreateOrganizationInProject.Input()
            {
                Name = "Claim Value Organization",
                ManagerEmails = new string[1] { "claim-value@example.com" },
                ProjectId = project.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var organization = await DbContext.Organizations.FirstAsync();
            organization.Id.Should().BeGreaterThan(0);

            var manager = await UserManager.FindByEmailAsync("claim-value@example.com");
            var claim = (await UserManager.GetClaimsAsync(manager)).Should().ContainSingle(c => c.Type == AppClaimTypes.OrganizationManagerOf).Which;
            claim.Value.Should().Be(organization.Id.ToString());
        }

        [Fact]
        public async Task When_ManagerAlreadyExists_AssignsClaimWithRealOrganizationId()
        {
            var existingManager = AddUser("existing-org-manager@example.com", UserType.OrganizationManager);

            var input = new CreateOrganizationInProject.Input()
            {
                Name = "Existing Manager Organization",
                ManagerEmails = new string[1] { existingManager.Email },
                ProjectId = project.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var organization = await DbContext.Organizations.FirstAsync();
            organization.Id.Should().BeGreaterThan(0);

            var claim = (await UserManager.GetClaimsAsync(existingManager)).Should().ContainSingle(c => c.Type == AppClaimTypes.OrganizationManagerOf).Which;
            claim.Value.Should().Be(organization.Id.ToString());
        }
    }
}
