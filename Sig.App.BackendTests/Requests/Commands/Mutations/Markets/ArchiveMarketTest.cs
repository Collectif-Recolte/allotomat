using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Markets;
using Sig.App.Backend.Services.Mailer;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Markets
{
    public class ArchiveMarketTest : TestBase
    {
        private readonly IRequestHandler<ArchiveMarket.Input> handler;
        private Mock<IMailer> mailer;
        private readonly Market market;
        private readonly MarketGroup marketGroup;

        public ArchiveMarketTest()
        {
            market = new Market()
            {
                Name = "Market 1"
            };
            DbContext.Markets.Add(market);

            marketGroup = new MarketGroup()
            {
                Name = "Market Group 1",
                Markets = new List<MarketGroupMarket>()
            };
            marketGroup.Markets.Add(new MarketGroupMarket() { Market = market, MarketGroup = marketGroup });
            DbContext.MarketGroups.Add(marketGroup);

            DbContext.SaveChanges();

            mailer = new Mock<IMailer>();
            handler = new ArchiveMarket(NullLogger<ArchiveMarket>.Instance, DbContext, UserManager, Mediator);
        }

        [Fact]
        public async Task ArchiveTheMarket()
        {
            var input = new ArchiveMarket.Input()
            {
                MarketId = market.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var marketCount = await DbContext.Markets.CountAsync();
            marketCount.Should().Be(1);

            var localMarket = await DbContext.Markets.FirstAsync();
            localMarket.IsArchived.Should().BeTrue();

            var marketGroupMarketCount = await DbContext.MarketGroupMarkets.CountAsync();
            marketGroupMarketCount.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfMarketNotFound()
        {
            var input = new ArchiveMarket.Input()
            {
                MarketId = Id.New<Market>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ArchiveMarket.MarketNotFoundException>();
        }
    }
}
