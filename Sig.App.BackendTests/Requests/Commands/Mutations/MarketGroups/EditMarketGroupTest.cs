using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Requests.Commands.Mutations.MarketGroups;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.MarketGroups
{
    public class EditMarketGroupTest : TestBase
    {
        private readonly EditMarketGroup handler;
        private readonly MarketGroup MarketGroup;
        public EditMarketGroupTest()
        {
            MarketGroup = new MarketGroup()
            {
                Name = "Regroupement marché"
            };
            DbContext.MarketGroups.Add(MarketGroup);

            DbContext.SaveChanges();

            handler = new EditMarketGroup(NullLogger<EditMarketGroup>.Instance, DbContext);
        }

        [Fact]
        public async Task EditTheMarketGroup()
        {
            var input = new EditMarketGroup.Input()
            {
                MarketGroupId = MarketGroup.GetIdentifier(),
                Name = new Maybe<NonNull<string>>("Regroupement marché 1 test"),
            };

            await handler.Handle(input, CancellationToken.None);

            var localMarketGroup = await DbContext.MarketGroups.FirstAsync();

            localMarketGroup.Name.Should().Be("Regroupement marché 1 test");
        }

        [Fact]
        public async Task ThrowsIfMarketGroupNotFound()
        {
            var input = new EditMarketGroup.Input()
            {
                MarketGroupId = Id.New<MarketGroup>(123456),
                Name = new Maybe<NonNull<string>>("Regroupement marché test")
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditMarketGroup.MarketGroupNotFoundException>();
        }
    }
}
