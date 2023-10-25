using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Requests.Commands.Mutations.Projects;
using Sig.App.Backend.Services.Mailer;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Projects
{
    public class CreateProjectTest : TestBase
    {
        private readonly CreateProject handler;
        private Mock<IMailer> mailer;

        public CreateProjectTest()
        {
            mailer = new Mock<IMailer>();
            handler = new CreateProject(NullLogger<CreateProject>.Instance, DbContext, UserManager, mailer.Object);
        }

        [Fact]
        public async Task CreatesTheProject()
        {
            var input = new CreateProject.Input()
            {
                Name = "Project Test 1",
                Url = "https://www.example.com/",
                ManagerEmails = new string[1] { "test1@example.com" }
            };

            await handler.Handle(input, CancellationToken.None);

            var project = await DbContext.Projects.FirstAsync();

            project.Name.Should().Be("Project Test 1");
            project.Url.Should().Be("https://www.example.com/");
            project.AllowOrganizationsAssignCards.Should().BeFalse();
        }

        [Fact]
        public async Task CreatesTheProjectAndAllowOrganizationsAssignCards()
        {
            var input = new CreateProject.Input()
            {
                Name = "Project Test 1",
                Url = "https://www.example.com/",
                ManagerEmails = new string[1] { "test1@example.com" },
                AllowOrganizationsAssignCards = true
            };

            await handler.Handle(input, CancellationToken.None);

            var project = await DbContext.Projects.FirstAsync();

            project.Name.Should().Be("Project Test 1");
            project.Url.Should().Be("https://www.example.com/");
            project.AllowOrganizationsAssignCards.Should().BeTrue();
        }

        [Fact]
        public async Task SendsConfirmationEmail()
        {
            var input = new CreateProject.Input()
            {
                Name = "Project Test 1",
                Url = "https://www.example.com/",
                ManagerEmails = new string[1] { "test1@example.com" }
            };

            await handler.Handle(input, CancellationToken.None);

            mailer.Verify(x => x.Send(It.IsAny<ProjectManagerInviteEmail>()));
        }
    }
}
