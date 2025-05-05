using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Requests.Commands.Mutations.Markets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Markets
{
    public class EnabledMarketTest : TestBase
    {
        private readonly EnabledMarket handler;
        private readonly Market market;
        public EnabledMarketTest()
        {
            market = new Market()
            {
                Name = "Marché 1",
                IsDisabled = true
            };
            DbContext.Markets.Add(market);

            DbContext.SaveChanges();

            handler = new EnabledMarket(NullLogger<EnabledMarket>.Instance, DbContext);
        }

        [Fact]
        public async Task EnabledTheMarket()
        {
            var input = new EnabledMarket.Input()
            {
                MarketId = market.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localMarket = await DbContext.Markets.FirstAsync();

            localMarket.IsDisabled.Should().Be(false);
        }

        [Fact]
        public async Task ThrowsIfMarketNotFound()
        {
            var input = new EnabledMarket.Input()
            {
                MarketId = Id.New<Market>(123456),
                Name = new Maybe<NonNull<string>>("Marché 1 test")
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EnabledMarket.MarketNotFoundException>();
        }
    }
}
