using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.MarketGroups;
using Sig.App.Backend.Services.Mailer;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.MarketGroups
{
    public class AddManagerToMarketGroupTest : TestBase
    {
        private readonly AddManagerToMarketGroup handler;
        private readonly MarketGroup MarketGroup;
        private Mock<IMailer> mailer;

        public AddManagerToMarketGroupTest()
        {
            mailer = new Mock<IMailer>();

            MarketGroup = new MarketGroup()
            {
                Name = "MarketGroup 1"
            };
            DbContext.MarketGroups.Add(MarketGroup);

            DbContext.SaveChanges();

            handler = new AddManagerToMarketGroup(NullLogger<AddManagerToMarketGroup>.Instance, DbContext, UserManager, mailer.Object);
        }

        [Fact]
        public async Task AddMarketGroupToProject()
        {
            var input = new AddManagerToMarketGroup.Input()
            {
                MarketGroupId = MarketGroup.GetIdentifier(),
                ManagerEmails = new string[1] { "test1@example.com" }
            };

            await handler.Handle(input, CancellationToken.None);

            var firstUser = await DbContext.Users.FirstAsync();
            var claims = await UserManager.GetClaimsAsync(firstUser);

            firstUser.Email.Should().Be("test1@example.com");
            claims.Count.Should().Be(1);
        }

        [Fact]
        public async Task ThrowsIfMarketGroupNotFound()
        {
            var input = new AddManagerToMarketGroup.Input()
            {
                MarketGroupId = Id.New<MarketGroup>(123456),
                ManagerEmails = new string[1] { "test2@example.com" }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddManagerToMarketGroup.MarketGroupNotFoundException>();
        }
    }
}
