using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Services.Kiosk;
using Sig.App.Backend.Requests.Commands.Mutations.CashRegisters;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.CashRegisters
{
    public class AuthenticateKioskTest : TestBase
    {
        private const string SigningKey = "test-signing-key-at-least-32-chars-long";

        private readonly IRequestHandler<AuthenticateKiosk.Input, AuthenticateKiosk.Payload> handler;
        private readonly CashRegister cashRegister;

        public AuthenticateKioskTest()
        {
            handler = new AuthenticateKiosk(
                NullLogger<AuthenticateKiosk>.Instance,
                DbContext,
                new KioskJwtService(Options.Create(new KioskJwtOptions { SigningKey = SigningKey })));

            cashRegister = CreateOperationalKiosk("kiosk-slug", "ABCD1234").GetAwaiter().GetResult();
        }

        [Fact]
        public async Task ReturnsJwtForValidPassword()
        {
            var result = await handler.Handle(new AuthenticateKiosk.Input
            {
                Token = cashRegister.KioskAccessToken,
                Password = "abcd1234"
            }, CancellationToken.None);

            result.AccessToken.Should().NotBeNullOrEmpty();
            result.ExpiresAt.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ThrowsForInvalidPassword()
        {
            await F(() => handler.Handle(new AuthenticateKiosk.Input
            {
                Token = cashRegister.KioskAccessToken,
                Password = "WRONGPWD"
            }, CancellationToken.None))
                .Should().ThrowAsync<AuthenticateKiosk.KioskAuthenticationFailedException>();
        }

        [Fact]
        public async Task ThrowsForUnknownToken()
        {
            await F(() => handler.Handle(new AuthenticateKiosk.Input
            {
                Token = "unknown-token",
                Password = "ABCD1234"
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
