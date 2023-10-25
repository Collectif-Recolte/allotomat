using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Cards;
using Sig.App.Backend.Services.Cards;
using Sig.App.Backend.Services.Mailer;
using Sig.App.Backend.Services.QRCode;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Cards
{
    public class CreateCardsTest : TestBase
    {
        private readonly CreateCards handler;
        private readonly Project project;

        public CreateCardsTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            DbContext.SaveChanges();

            var qrCodeService = new Mock<IQRCodeService>();
            var cardService = new Mock<ICardService>();
            var mailer = new Mock<IMailer>();
            
            handler = new CreateCards(NullLogger<CreateCards>.Instance, DbContext, qrCodeService.Object, mailer.Object, Mediator, cardService.Object);
        }

        [Fact]
        public async Task Create10Cards()
        {
            var input = new CreateCards.Input()
            {
                ProjectId = project.GetIdentifier(),
                Count = 10
            };

            await handler.Handle(input, CancellationToken.None);

            var cards = DbContext.Cards.ToList();

            cards.Count.Should().Be(10);
        }

        [Fact]
        public async Task ThrowsIfProjectNotFound()
        {
            var input = new CreateCards.Input()
            {
                ProjectId = Id.New<Project>(123456),
                Count = 10
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateCards.ProjectNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfCountLessThan1()
        {
            var input = new CreateCards.Input()
            {
                ProjectId = project.GetIdentifier(),
                Count = 0
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateCards.CountMustBeHigherThanZeroException>();
        }
    }
}
