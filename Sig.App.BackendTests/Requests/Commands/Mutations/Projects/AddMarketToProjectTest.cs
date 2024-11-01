using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Projects;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Projects
{
    public class AddMarketToProjectTest : TestBase
    {
        private readonly AddMarketToProject handler;
        private readonly Market market;
        private readonly Project project;
        private readonly MarketGroup marketGroup;

        public AddMarketToProjectTest()
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
            DbContext.Projects.Add(project);

            marketGroup = new MarketGroup()
            {
                Project = project,
                Name = "MarketGroup 1"
            };
            DbContext.MarketGroups.Add(marketGroup);

            DbContext.SaveChanges();

            handler = new AddMarketToProject(NullLogger<AddMarketToProject>.Instance, DbContext, Mediator);
        }

        [Fact]
        public async Task AddMarketToProject()
        {
            var input = new AddMarketToProject.Input()
            {
                MarketId = market.GetIdentifier(),
                ProjectId = project.GetIdentifier(),
                MarketGroupId = marketGroup.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localProject = await DbContext.Projects.FirstAsync();
            var localMarket = await DbContext.Markets.FirstAsync();

            localProject.Markets.Should().HaveCount(1);
            localMarket.Projects.Should().HaveCount(1);
        }

        [Fact]
        public async Task ThrowsIfProjectNotFound()
        {
            var input = new AddMarketToProject.Input()
            {
                MarketId = market.GetIdentifier(),
                ProjectId = Id.New<Project>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMarketToProject.ProjectNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfMarketNotFound()
        {
            var input = new AddMarketToProject.Input()
            {
                MarketId = Id.New<Market>(123456),
                ProjectId = project.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMarketToProject.MarketNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfMarketAlreadyInProject()
        {
            var projectMarket = new ProjectMarket()
            {
                Market = market,
                Project = project
            };
            DbContext.ProjectMarkets.Add(projectMarket);

            var input = new AddMarketToProject.Input()
            {
                MarketId = market.GetIdentifier(),
                ProjectId = project.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMarketToProject.MarketAlreadyInProjectException>();
        }
    }
}
