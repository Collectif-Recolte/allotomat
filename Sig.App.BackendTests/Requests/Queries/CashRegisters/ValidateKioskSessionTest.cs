using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Options;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Requests.Queries.CashRegisters;
using Sig.App.Backend.Services.Kiosk;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Queries.CashRegisters
{
    public class ValidateKioskSessionTest : TestBase
    {
        private const string SigningKey = "test-signing-key-at-least-32-chars-long";

        private readonly IRequestHandler<ValidateKioskSession.Input, bool> handler;

        public ValidateKioskSessionTest()
        {
            handler = new ValidateKioskSession(
                DbContext,
                new KioskJwtService(Options.Create(new KioskJwtOptions { SigningKey = SigningKey })));
        }

        [Fact]
        public async Task ReturnsTrueForValidJwt()
        {
            var cashRegister = await CreateOperationalKiosk("kiosk-slug-123", "ABCD1234");
            var jwtService = new KioskJwtService(Options.Create(new KioskJwtOptions { SigningKey = SigningKey }));
            var (jwt, _) = jwtService.IssueToken(cashRegister.KioskAccessToken);

            var result = await handler.Handle(new ValidateKioskSession.Input { KioskToken = jwt }, CancellationToken.None);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ThrowsForInvalidJwt()
        {
            await F(() => handler.Handle(new ValidateKioskSession.Input { KioskToken = "invalid" }, CancellationToken.None))
                .Should().ThrowAsync<KioskAccessInvalidException>();
        }

        [Fact]
        public async Task ThrowsAfterTokenRegeneration()
        {
            var cashRegister = await CreateOperationalKiosk("old-slug", "ABCD1234");
            var jwtService = new KioskJwtService(Options.Create(new KioskJwtOptions { SigningKey = SigningKey }));
            var (jwt, _) = jwtService.IssueToken(cashRegister.KioskAccessToken);

            cashRegister.KioskAccessToken = KioskHelper.GenerateAccessToken();
            cashRegister.KioskPassword = KioskHelper.GeneratePassword();
            await DbContext.SaveChangesAsync();

            await F(() => handler.Handle(new ValidateKioskSession.Input { KioskToken = jwt }, CancellationToken.None))
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
