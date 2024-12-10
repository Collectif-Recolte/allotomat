using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.Extensions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Sig.App.Backend.Requests.Commands.Mutations.MarketGroups;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.MarketGroups
{
    public class RemoveMarketFromMarketGroupTest : TestBase
    {
        private readonly RemoveMarketFromMarketGroup handler;
        private readonly Market market;
        private readonly MarketGroup MarketGroup;

        public RemoveMarketFromMarketGroupTest()
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

            MarketGroup.Markets = new List<MarketGroupMarket>() { new MarketGroupMarket() { Market = market, MarketGroup = MarketGroup } };

            DbContext.MarketGroups.Add(MarketGroup);

            DbContext.SaveChanges();

            handler = new RemoveMarketFromMarketGroup(NullLogger<RemoveMarketFromMarketGroup>.Instance, DbContext);
        }

        [Fact]
        public async Task RemoveMarketFromMarketGroup()
        {
            var input = new RemoveMarketFromMarketGroup.Input()
            {
                MarketId = market.GetIdentifier(),
                MarketGroupId = MarketGroup.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localMarketGroupMarket = await DbContext.MarketGroupMarkets.CountAsync();

            localMarketGroupMarket.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfMarketGroupNotFound()
        {
            var input = new RemoveMarketFromMarketGroup.Input()
            {
                MarketId = market.GetIdentifier(),
                MarketGroupId = Id.New<MarketGroup>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveMarketFromMarketGroup.MarketGroupNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfMarketNotFound()
        {
            var input = new RemoveMarketFromMarketGroup.Input()
            {
                MarketId = Id.New<Market>(123456),
                MarketGroupId = MarketGroup.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveMarketFromMarketGroup.MarketNotFoundException>();
        }
    }
}
