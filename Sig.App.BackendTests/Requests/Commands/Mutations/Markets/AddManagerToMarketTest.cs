using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Markets;
using Sig.App.Backend.Services.Mailer;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Markets
{
    public class AddManagerToMarketTest : TestBase
    {
        private readonly AddManagerToMarket handler;
        private readonly Market market;
        private Mock<IMailer> mailer;

        public AddManagerToMarketTest()
        {
            mailer = new Mock<IMailer>();

            market = new Market()
            {
                Name = "Market 1"
            };
            DbContext.Markets.Add(market);

            DbContext.SaveChanges();

            handler = new AddManagerToMarket(NullLogger<AddManagerToMarket>.Instance, DbContext, UserManager, mailer.Object);
        }

        [Fact]
        public async Task AddMarketToProject()
        {
            var input = new AddManagerToMarket.Input()
            {
                MarketId = market.GetIdentifier(),
                ManagerEmails = new string[1] { "test1@example.com" }
            };

            await handler.Handle(input, CancellationToken.None);

            var firstUser = await DbContext.Users.FirstAsync();
            var claims = await UserManager.GetClaimsAsync(firstUser);

            firstUser.Email.Should().Be("test1@example.com");
            claims.Count.Should().Be(1);
        }

        [Fact]
        public async Task ThrowsIfMarketNotFound()
        {
            var input = new AddManagerToMarket.Input()
            {
                MarketId = Id.New<Market>(123456),
                ManagerEmails = new string[1] { "test2@example.com" }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddManagerToMarket.MarketNotFoundException>();
        }
    }
}
