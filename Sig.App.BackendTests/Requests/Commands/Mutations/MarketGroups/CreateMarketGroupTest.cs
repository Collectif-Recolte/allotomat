using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.MarketGroups;
using Sig.App.Backend.Services.Mailer;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.MarketGroups
{
    public class CreateMarketGroupTest : TestBase
    {
        private readonly CreateMarketGroup handler;
        private readonly Project project;
        private Mock<IMailer> mailer;

        public CreateMarketGroupTest()
        {
            mailer = new Mock<IMailer>();

            project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            DbContext.SaveChanges();

            handler = new CreateMarketGroup(NullLogger<CreateMarketGroup>.Instance, DbContext, UserManager, mailer.Object);
        }

        [Fact]
        public async Task CreateTheMarketGroup()
        {
            var input = new CreateMarketGroup.Input()
            {
                Name = "MarketGroup Test 1",
                ManagerEmails = new string[1] { "test1@example.com" },
                ProjectId = project.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var MarketGroup = await DbContext.MarketGroups.FirstAsync();

            MarketGroup.Name.Should().Be("MarketGroup Test 1");
        }

        [Fact]
        public async Task SendsConfirmationEmail()
        {
            var input = new CreateMarketGroup.Input()
            {
                Name = "MarketGroup Test 1",
                ManagerEmails = new string[1] { "test1@example.com" },
                ProjectId = project.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            mailer.Verify(x => x.Send(It.IsAny<MarketGroupManagerInviteEmail>()));
        }
    }
}
