using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.MarketGroups;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.MarketGroups
{
    public class AddMarketToMarketGroupTest : TestBase
    {
        private readonly AddMarketToMarketGroup handler;
        private readonly Market market;
        private readonly MarketGroup MarketGroup;

        public AddMarketToMarketGroupTest()
        {
            market = new Market()
            {
                Name = "Market 1"
            };
            DbContext.Markets.Add(market);

            MarketGroup = new MarketGroup()
            {
                Name = "MarketGroup 1"
            };
            DbContext.MarketGroups.Add(MarketGroup);

            DbContext.SaveChanges();

            handler = new AddMarketToMarketGroup(NullLogger<AddMarketToMarketGroup>.Instance, DbContext);
        }

        [Fact]
        public async Task AddMarketToMarketGroup()
        {
            var input = new AddMarketToMarketGroup.Input()
            {
                MarketId = market.GetIdentifier(),
                MarketGroupId = MarketGroup.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localMarketGroup = await DbContext.MarketGroups.FirstAsync();
            var localMarket = await DbContext.Markets.FirstAsync();

            localMarketGroup.Markets.Should().HaveCount(1);
            localMarket.MarketGroups.Should().HaveCount(1);
        }

        [Fact]
        public async Task ThrowsIfProjectNotFound()
        {
            var input = new AddMarketToMarketGroup.Input()
            {
                MarketId = market.GetIdentifier(),
                MarketGroupId = Id.New<MarketGroup>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMarketToMarketGroup.MarketGroupNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfMarketNotFound()
        {
            var input = new AddMarketToMarketGroup.Input()
            {
                MarketId = Id.New<Market>(123456),
                MarketGroupId = MarketGroup.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMarketToMarketGroup.MarketNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfMarketAlreadyInProject()
        {
            var MarketGroupMarket = new MarketGroupMarket()
            {
                Market = market,
                MarketGroup = MarketGroup
            };
            DbContext.MarketGroupMarkets.Add(MarketGroupMarket);

            var input = new AddMarketToMarketGroup.Input()
            {
                MarketId = market.GetIdentifier(),
                MarketGroupId = MarketGroup.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMarketToMarketGroup.MarketAlreadyInMarketGroupException>();
        }
    }
}
