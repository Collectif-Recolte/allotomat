using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Projects;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Projects
{
    public class RemoveMarketFromProjectTest : TestBase
    {
        private readonly RemoveMarketFromProject handler;
        private readonly Market market;
        private readonly Project project;

        public RemoveMarketFromProjectTest()
        {
            market = new Market()
            {
                Name = "Market 1"
            };
            DbContext.Markets.Add(market);

            project = new Project()
            {
                Name = "Project 1"
            };

            project.Markets = new List<ProjectMarket>() { new ProjectMarket() { Market = market, Project = project } };

            DbContext.Projects.Add(project);

            DbContext.SaveChanges();

            handler = new RemoveMarketFromProject(NullLogger<RemoveMarketFromProject>.Instance, DbContext);
        }

        [Fact]
        public async Task RemoveMarketFromProject()
        {
            var input = new RemoveMarketFromProject.Input()
            {
                MarketId = market.GetIdentifier(),
                ProjectId = project.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localProjectMarket = await DbContext.ProjectMarkets.CountAsync();

            localProjectMarket.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfProjectNotFound()
        {
            var input = new RemoveMarketFromProject.Input()
            {
                MarketId = market.GetIdentifier(),
                ProjectId = Id.New<Project>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveMarketFromProject.ProjectNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfMarketNotFound()
        {
            var input = new RemoveMarketFromProject.Input()
            {
                MarketId = Id.New<Market>(123456),
                ProjectId = project.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveMarketFromProject.MarketNotFoundException>();
        }
    }
}
