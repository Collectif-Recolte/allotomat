using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.MarketGroups;
using Sig.App.Backend.Services.Mailer;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.MarketGroups
{
    public class DeleteMarketGroupTest : TestBase
    {
        private readonly IRequestHandler<DeleteMarketGroup.Input> handler;
        private Mock<IMailer> mailer;
        private readonly MarketGroup MarketGroup;

        public DeleteMarketGroupTest()
        {
            MarketGroup = new MarketGroup()
            {
                Name = "MarketGroup 1"
            };
            DbContext.MarketGroups.Add(MarketGroup);

            DbContext.SaveChanges();

            mailer = new Mock<IMailer>();
            handler = new DeleteMarketGroup(NullLogger<DeleteMarketGroup>.Instance, DbContext, UserManager, Mediator);
        }

        [Fact]
        public async Task DeleteTheMarketGroup()
        {
            var input = new DeleteMarketGroup.Input()
            {
                MarketGroupId = MarketGroup.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var MarketGroupCount = await DbContext.MarketGroups.CountAsync();
            MarketGroupCount.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfMarketGroupNotFound()
        {
            var input = new DeleteMarketGroup.Input()
            {
                MarketGroupId = Id.New<MarketGroup>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteMarketGroup.MarketGroupNotFoundException>();
        }
    }
}
