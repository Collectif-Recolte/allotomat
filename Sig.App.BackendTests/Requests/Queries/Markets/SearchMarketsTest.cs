using FluentAssertions;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.Requests.Queries.Markets;
using Sig.App.Backend.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Queries.Markets
{
    public class SearchMarketsTest : TestBase
    {
        private readonly SearchMarkets handler;
        private readonly Market activeMarket;
        private readonly Market archivedMarket;
        private readonly MarketGroup marketGroup;

        public SearchMarketsTest()
        {
            activeMarket = new Market { Name = "Active Market" };
            archivedMarket = new Market { Name = "Archived Market", IsArchived = true };
            marketGroup = new MarketGroup { Name = "Market Group 1" };

            marketGroup.Markets = new List<MarketGroupMarket>
            {
                new MarketGroupMarket { Market = activeMarket, MarketGroup = marketGroup },
                new MarketGroupMarket { Market = archivedMarket, MarketGroup = marketGroup }
            };

            DbContext.Markets.AddRange(activeMarket, archivedMarket);
            DbContext.MarketGroups.Add(marketGroup);
            DbContext.SaveChanges();

            handler = new SearchMarkets(DbContext);
        }

        [Fact]
        public async Task ExcludesArchivedMarketsWhenSearchingByMarketGroup()
        {
            var result = await handler.Handle(new SearchMarkets.Query
            {
                MarketGroupId = marketGroup.Id,
                Page = new Page(1, 30)
            }, CancellationToken.None);

            result.Items.Should().ContainSingle()
                .Which.Id.Should().Be(activeMarket.Id);
            result.Items.Should().NotContain(x => x.IsArchived);
        }

        [Fact]
        public async Task IncludesArchivedMarketsWhenSearchingAllMarkets()
        {
            var result = await handler.Handle(new SearchMarkets.Query
            {
                Page = new Page(1, 30)
            }, CancellationToken.None);

            result.Items.Should().HaveCount(2);
            result.Items.Select(x => x.Id).Should().Contain(new[] { activeMarket.Id, archivedMarket.Id });
        }
    }
}
