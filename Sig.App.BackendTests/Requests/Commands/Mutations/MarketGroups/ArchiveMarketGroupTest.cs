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
    public class ArchiveMarketGroupTest : TestBase
    {
        private readonly IRequestHandler<ArchiveMarketGroup.Input> handler;
        private Mock<IMailer> mailer;
        private readonly MarketGroup MarketGroup;

        public ArchiveMarketGroupTest()
        {
            MarketGroup = new MarketGroup()
            {
                Name = "MarketGroup 1"
            };
            DbContext.MarketGroups.Add(MarketGroup);

            DbContext.SaveChanges();

            mailer = new Mock<IMailer>();
            handler = new ArchiveMarketGroup(NullLogger<ArchiveMarketGroup>.Instance, DbContext, UserManager, Mediator);
        }

        [Fact]
        public async Task ArchiveTheMarketGroup()
        {
            var input = new ArchiveMarketGroup.Input()
            {
                MarketGroupId = MarketGroup.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var MarketGroupCount = await DbContext.MarketGroups.CountAsync();
            MarketGroupCount.Should().Be(1);

            var localMarketGroup = await DbContext.MarketGroups.FirstAsync();
            localMarketGroup.IsArchived.Should().BeTrue();
        }

        [Fact]
        public async Task ThrowsIfMarketGroupNotFound()
        {
            var input = new ArchiveMarketGroup.Input()
            {
                MarketGroupId = Id.New<MarketGroup>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ArchiveMarketGroup.MarketGroupNotFoundException>();
        }
    }
}
