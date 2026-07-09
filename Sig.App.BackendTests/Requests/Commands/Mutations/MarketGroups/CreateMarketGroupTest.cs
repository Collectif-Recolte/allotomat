using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
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

        [Fact]
        public async Task AssignsManagerClaimWithRealMarketGroupId()
        {
            var input = new CreateMarketGroup.Input()
            {
                Name = "Claim Value MarketGroup",
                ManagerEmails = new string[1] { "claim-value@example.com" },
                ProjectId = project.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var marketGroup = await DbContext.MarketGroups.FirstAsync();
            marketGroup.Id.Should().BeGreaterThan(0);

            var manager = await UserManager.FindByEmailAsync("claim-value@example.com");
            var claim = (await UserManager.GetClaimsAsync(manager)).Should().ContainSingle(c => c.Type == AppClaimTypes.MarketGroupManagerOf).Which;
            claim.Value.Should().Be(marketGroup.Id.ToString());
        }

        [Fact]
        public async Task When_ManagerAlreadyExists_AssignsClaimWithRealMarketGroupId()
        {
            var existingManager = AddUser("existing-mg-manager@example.com", UserType.MarketGroupManager);

            var input = new CreateMarketGroup.Input()
            {
                Name = "Existing Manager MarketGroup",
                ManagerEmails = new string[1] { existingManager.Email },
                ProjectId = project.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var marketGroup = await DbContext.MarketGroups.FirstAsync();
            marketGroup.Id.Should().BeGreaterThan(0);

            var claim = (await UserManager.GetClaimsAsync(existingManager)).Should().ContainSingle(c => c.Type == AppClaimTypes.MarketGroupManagerOf).Which;
            claim.Value.Should().Be(marketGroup.Id.ToString());
        }
    }
}
