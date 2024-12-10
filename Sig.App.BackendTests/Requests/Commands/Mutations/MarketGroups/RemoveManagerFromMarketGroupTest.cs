using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.MarketGroups;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.MarketGroups
{
    public class RemoveManagerFromMarketGroupTest : TestBase
    {
        private readonly RemoveManagerFromMarketGroup handler;
        private readonly MarketGroup MarketGroup;
        private readonly AppUser manager;

        public RemoveManagerFromMarketGroupTest()
        {
            MarketGroup = new MarketGroup()
            {
                Name = "MarketGroup 1"
            };
            DbContext.MarketGroups.Add(MarketGroup);

            manager = new AppUser("test1@example.com")
            {
                Type = UserType.Merchant,
                Profile = new UserProfile()
            };

            UserManager.CreateAsync(manager);
            UserManager.AddClaimAsync(manager, new Claim(AppClaimTypes.MarketGroupManagerOf, MarketGroup.Id.ToString()));

            DbContext.SaveChanges();

            handler = new RemoveManagerFromMarketGroup(NullLogger<RemoveManagerFromMarketGroup>.Instance, DbContext, UserManager);
        }

        [Fact]
        public async Task RemoveManagerFromMarketGroup()
        {
            var input = new RemoveManagerFromMarketGroup.Input()
            {
                MarketGroupId = MarketGroup.GetIdentifier(),
                ManagerId = manager.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var firstUser = await DbContext.Users.FirstAsync();
            var claims = await UserManager.GetClaimsAsync(firstUser);

            firstUser.Email.Should().Be("test1@example.com");
            claims.Count.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfMarketGroupNotFound()
        {
            var input = new RemoveManagerFromMarketGroup.Input()
            {
                MarketGroupId = Id.New<MarketGroup>(123456),
                ManagerId = manager.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveManagerFromMarketGroup.MarketGroupNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfManagerNotFound()
        {
            var input = new RemoveManagerFromMarketGroup.Input()
            {
                MarketGroupId = MarketGroup.GetIdentifier(),
                ManagerId = Id.New<AppUser>(123456),
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveManagerFromMarketGroup.ManagerNotFoundException>();
        }
    }
}
