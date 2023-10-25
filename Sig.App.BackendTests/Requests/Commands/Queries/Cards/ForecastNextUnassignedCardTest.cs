using FluentAssertions;
using GraphQL.Conventions;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Queries.Cards;
using Sig.App.Backend.Requests.Queries.Beneficiaries;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Queries.Cards
{
    public class ForecastNextUnassignedCardTest : TestBase
    {
        private readonly ForecastNextUnassignedCard handler;

        private readonly Project project;

        private readonly Card card1;
        private readonly Card card2;

        public ForecastNextUnassignedCardTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            card1 = new Card()
            {
                Project = project,
                ProgramCardId = 1
            };
            DbContext.Cards.Add(card1);

            card2 = new Card()
            {
                Project = project,
                ProgramCardId = 2
            };
            DbContext.Cards.Add(card2);

            DbContext.SaveChanges();

            handler = new ForecastNextUnassignedCard(DbContext);
        }

        [Fact]
        public async Task ForecastNextUnassignedCard()
        {
            var input = new ForecastNextUnassignedCard.Input()
            {
                ProjectId = project.GetIdentifier()
            };

            var result = await handler.Handle(input, CancellationToken.None);

            result.Should().Be(1);
        }

        [Fact]
        public async Task ForecastNextUnassignedCard2()
        {
            card1.Status = Backend.DbModel.Enums.CardStatus.Deactivated;

            DbContext.SaveChanges();

            var input = new ForecastNextUnassignedCard.Input()
            {
                ProjectId = project.GetIdentifier()
            };

            var result = await handler.Handle(input, CancellationToken.None);

            result.Should().Be(2);
        }

        [Fact]
        public async Task ThrowsIfProjectNotFound()
        {
            var input = new ForecastNextUnassignedCard.Input()
            {
                ProjectId = Id.New<Project>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ForecastNextUnassignedCard.ProjectNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfProjectNoCardAvailable()
        {
            card1.Status = Backend.DbModel.Enums.CardStatus.Deactivated;
            card2.Status = Backend.DbModel.Enums.CardStatus.Deactivated;

            DbContext.SaveChanges();

            var input = new ForecastNextUnassignedCard.Input()
            {
                ProjectId = project.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ForecastNextUnassignedCard.NoUnassignedCardAvailableException>();
        }
    }
}
