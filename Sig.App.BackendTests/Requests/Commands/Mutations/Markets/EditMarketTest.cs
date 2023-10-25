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
    public class EditMarketTest : TestBase
    {
        private readonly EditMarket handler;
        private readonly Market market;
        public EditMarketTest()
        {
            market = new Market()
            {
                Name = "Marché 1"
            };
            DbContext.Markets.Add(market);

            DbContext.SaveChanges();

            handler = new EditMarket(NullLogger<EditMarket>.Instance, DbContext);
        }

        [Fact]
        public async Task EditTheMarket()
        {
            var input = new EditMarket.Input()
            {
                MarketId = market.GetIdentifier(),
                Name = new Maybe<NonNull<string>>("Marché 1 test"),
            };

            await handler.Handle(input, CancellationToken.None);

            var localMarket = await DbContext.Markets.FirstAsync();

            localMarket.Name.Should().Be("Marché 1 test");
        }

        [Fact]
        public async Task ThrowsIfMarketNotFound()
        {
            var input = new EditMarket.Input()
            {
                MarketId = Id.New<Market>(123456),
                Name = new Maybe<NonNull<string>>("Marché 1 test")
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditMarket.MarketNotFoundException>();
        }
    }
}
