using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Services.Kiosk;
using Sig.App.Backend.Requests.Queries.Cards;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Queries.Cards
{
    public class VerifyCardCanBeUsedInKioskTest : TestBase
    {
        private const string SigningKey = "test-signing-key-at-least-32-chars-long";

        private readonly IRequestHandler<VerifyCardCanBeUsedInKiosk.Input, bool> handler;
        private readonly CashRegister cashRegister;
        private readonly string jwt;

        public VerifyCardCanBeUsedInKioskTest()
        {
            var jwtService = new KioskJwtService(Options.Create(new KioskJwtOptions { SigningKey = SigningKey }), NullLogger<KioskJwtService>.Instance);
            handler = new VerifyCardCanBeUsedInKiosk(DbContext, Mediator, jwtService);
            cashRegister = CreateOperationalKiosk("kiosk-slug", "ABCD1234").GetAwaiter().GetResult();
            jwt = jwtService.IssueToken(cashRegister.KioskAccessToken).AccessToken;

            MediatorMock
                .Setup(x => x.Send(It.IsAny<VerifyCardCanBeUsedInMarket.Input>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        [Fact]
        public async Task AcceptsValidJwt()
        {
            var result = await handler.Handle(new VerifyCardCanBeUsedInKiosk.Input
            {
                KioskToken = jwt,
                CardId = Id.New<Backend.DbModel.Entities.Cards.Card>(1)
            }, CancellationToken.None);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task RejectsSlugInsteadOfJwt()
        {
            await F(() => handler.Handle(new VerifyCardCanBeUsedInKiosk.Input
            {
                KioskToken = cashRegister.KioskAccessToken,
                CardId = Id.New<Backend.DbModel.Entities.Cards.Card>(1)
            }, CancellationToken.None))
                .Should().ThrowAsync<KioskAccessInvalidException>();
        }

        private async Task<CashRegister> CreateOperationalKiosk(string kioskToken, string password)
        {
            var project = new Project { Name = "Programme test" };
            var market = new Market { Name = "Marché test" };
            var marketGroup = new MarketGroup { Name = "Groupe", Project = project };
            var register = new CashRegister
            {
                Name = "Caisse 1",
                Market = market,
                KioskAccessToken = kioskToken,
                KioskPassword = password
            };

            DbContext.Projects.Add(project);
            DbContext.Markets.Add(market);
            DbContext.MarketGroups.Add(marketGroup);
            DbContext.CashRegisters.Add(register);
            await DbContext.SaveChangesAsync();

            DbContext.CashRegisterMarketGroups.Add(new CashRegisterMarketGroup
            {
                CashRegisterId = register.Id,
                MarketGroupId = marketGroup.Id
            });
            await DbContext.SaveChangesAsync();

            return register;
        }
    }
}
