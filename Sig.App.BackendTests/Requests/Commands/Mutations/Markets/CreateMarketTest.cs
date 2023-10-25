using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Requests.Commands.Mutations.Markets;
using Sig.App.Backend.Services.Mailer;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Markets
{
    public class CreateMarketTest : TestBase
    {
        private readonly CreateMarket handler;
        private Mock<IMailer> mailer;

        public CreateMarketTest()
        {
            mailer = new Mock<IMailer>();
            handler = new CreateMarket(NullLogger<CreateMarket>.Instance, DbContext, UserManager, mailer.Object);
        }

        [Fact]
        public async Task CreateTheMarket()
        {
            var input = new CreateMarket.Input()
            {
                Name = "Market Test 1",
                ManagerEmails = new string[1] { "test1@example.com" }
            };

            await handler.Handle(input, CancellationToken.None);

            var market = await DbContext.Markets.FirstAsync();

            market.Name.Should().Be("Market Test 1");
        }

        [Fact]
        public async Task SendsConfirmationEmail()
        {
            var input = new CreateMarket.Input()
            {
                Name = "Market Test 1",
                ManagerEmails = new string[1] { "test1@example.com" }
            };

            await handler.Handle(input, CancellationToken.None);

            mailer.Verify(x => x.Send(It.IsAny<MarketManagerInviteEmail>()));
        }
    }
}
