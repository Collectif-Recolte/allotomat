using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Projects;
using Sig.App.Backend.Services.Mailer;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Projects
{
    public class AddManagerToProjectTest : TestBase
    {
        private readonly AddManagerToProject handler;
        private readonly Project project;
        private Mock<IMailer> mailer;

        public AddManagerToProjectTest()
        {
            mailer = new Mock<IMailer>();

            project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            DbContext.SaveChanges();

            handler = new AddManagerToProject(NullLogger<AddManagerToProject>.Instance, DbContext, UserManager, mailer.Object);
        }

        [Fact]
        public async Task AddMarketToProject()
        {
            var input = new AddManagerToProject.Input()
            {
                ProjectId = project.GetIdentifier(),
                ManagerEmails = new string[1] { "test1@example.com" }
            };

            await handler.Handle(input, CancellationToken.None);

            var firstUser = await DbContext.Users.FirstAsync();
            var claims = await UserManager.GetClaimsAsync(firstUser);

            firstUser.Email.Should().Be("test1@example.com");
            claims.Count.Should().Be(1);
        }

        [Fact]
        public async Task ThrowsIfProjectNotFound()
        {
            var input = new AddManagerToProject.Input()
            {
                ProjectId = Id.New<Project>(123456),
                ManagerEmails = new string[1] { "test2@example.com" }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddManagerToProject.ProjectNotFoundException>();
        }
    }
}
