using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Markets;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Markets
{
    public class RemoveManagerFromMarketTest : TestBase
    {
        private readonly RemoveManagerFromMarket handler;
        private readonly Market market;
        private readonly AppUser manager;

        public RemoveManagerFromMarketTest()
        {
            market = new Market()
            {
                Name = "Market 1"
            };
            DbContext.Markets.Add(market);

            manager = new AppUser("test1@example.com")
            {
                Type = UserType.Merchant,
                Profile = new UserProfile()
            };

            UserManager.CreateAsync(manager);
            UserManager.AddClaimAsync(manager, new Claim(AppClaimTypes.MarketManagerOf, market.Id.ToString()));

            DbContext.SaveChanges();

            handler = new RemoveManagerFromMarket(NullLogger<RemoveManagerFromMarket>.Instance, DbContext, UserManager);
        }

        [Fact]
        public async Task RemoveManagerFromMarket()
        {
            var input = new RemoveManagerFromMarket.Input()
            {
                MarketId = market.GetIdentifier(),
                ManagerId = manager.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var firstUser = await DbContext.Users.FirstAsync();
            var claims = await UserManager.GetClaimsAsync(firstUser);

            firstUser.Email.Should().Be("test1@example.com");
            claims.Count.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfMarketNotFound()
        {
            var input = new RemoveManagerFromMarket.Input()
            {
                MarketId = Id.New<Market>(123456),
                ManagerId = manager.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveManagerFromMarket.MarketNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfManagerNotFound()
        {
            var input = new RemoveManagerFromMarket.Input()
            {
                MarketId = market.GetIdentifier(),
                ManagerId = Id.New<AppUser>(123456),
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveManagerFromMarket.ManagerNotFoundException>();
        }
    }
}
