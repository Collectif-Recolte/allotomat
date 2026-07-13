using FluentAssertions;
using Microsoft.Extensions.Options;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Services.Kiosk;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Helpers
{
    public class KioskJwtServiceTest : TestBase
    {
        private const string SigningKey = "test-signing-key-at-least-32-chars-long";

        private readonly KioskJwtService service;

        public KioskJwtServiceTest()
        {
            service = new KioskJwtService(Options.Create(new KioskJwtOptions
            {
                SigningKey = SigningKey
            }));
        }

        [Fact]
        public void IssueToken_ReturnsNonEmptyToken()
        {
            var (accessToken, expiresAtUtc) = service.IssueToken("kiosk-slug-123");

            accessToken.Should().NotBeNullOrEmpty();
            expiresAtUtc.Should().BeAfter(System.DateTime.UtcNow);
        }

        [Fact]
        public async Task ResolveFromAuthToken_ReturnsOperationalKiosk()
        {
            var cashRegister = await CreateOperationalKiosk("kiosk-slug-123", "ABCD1234");
            var (jwt, _) = service.IssueToken(cashRegister.KioskAccessToken);

            var resolved = await service.ResolveFromAuthToken(DbContext, jwt, CancellationToken.None);

            resolved.TokenFound.Should().BeTrue();
            resolved.IsOperational.Should().BeTrue();
            resolved.CashRegister.Id.Should().Be(cashRegister.Id);
        }

        [Fact]
        public async Task ResolveFromAuthToken_ThrowsAfterTokenRegeneration()
        {
            var cashRegister = await CreateOperationalKiosk("old-slug", "ABCD1234");
            var (jwt, _) = service.IssueToken(cashRegister.KioskAccessToken);

            cashRegister.KioskAccessToken = KioskHelper.GenerateAccessToken();
            cashRegister.KioskPassword = KioskHelper.GeneratePassword();
            await DbContext.SaveChangesAsync();

            await F(() => service.ResolveFromAuthToken(DbContext, jwt, CancellationToken.None))
                .Should().ThrowAsync<KioskAccessInvalidException>();
        }

        private async Task<CashRegister> CreateOperationalKiosk(string kioskToken, string password)
        {
            var project = new Project { Name = "Programme test" };
            var market = new Market { Name = "Marché test" };
            var marketGroup = new MarketGroup { Name = "Groupe", Project = project };
            var cashRegister = new CashRegister
            {
                Name = "Caisse 1",
                Market = market,
                KioskAccessToken = kioskToken,
                KioskPassword = password
            };

            DbContext.Projects.Add(project);
            DbContext.Markets.Add(market);
            DbContext.MarketGroups.Add(marketGroup);
            DbContext.CashRegisters.Add(cashRegister);
            await DbContext.SaveChangesAsync();

            DbContext.CashRegisterMarketGroups.Add(new CashRegisterMarketGroup
            {
                CashRegisterId = cashRegister.Id,
                MarketGroupId = marketGroup.Id
            });
            await DbContext.SaveChangesAsync();

            return cashRegister;
        }
    }
}
