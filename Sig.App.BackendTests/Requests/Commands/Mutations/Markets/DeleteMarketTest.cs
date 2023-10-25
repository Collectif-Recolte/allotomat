using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
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
    public class DeleteMarketTest : TestBase
    {
        private readonly IRequestHandler<DeleteMarket.Input> handler;
        private Mock<IMailer> mailer;
        private readonly Market market;

        public DeleteMarketTest()
        {
            market = new Market()
            {
                Name = "Market 1"
            };
            DbContext.Markets.Add(market);

            DbContext.SaveChanges();

            mailer = new Mock<IMailer>();
            handler = new DeleteMarket(NullLogger<DeleteMarket>.Instance, DbContext, UserManager, Mediator);
        }

        [Fact]
        public async Task DeleteTheMarket()
        {
            var input = new DeleteMarket.Input()
            {
                MarketId = market.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var marketCount = await DbContext.Markets.CountAsync();
            marketCount.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfMarketNotFound()
        {
            var input = new DeleteMarket.Input()
            {
                MarketId = Id.New<Market>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteMarket.MarketNotFoundException>();
        }
    }
}
