using FluentAssertions;
using MediatR;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Services.Kiosk;
using Sig.App.Backend.Requests.Queries.CashRegisters;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Queries.CashRegisters
{
    public class GetCashRegisterByKioskTokenTest : TestBase
    {
        private readonly IRequestHandler<GetCashRegisterByKioskToken.Input, Backend.Gql.Schema.GraphTypes.KioskCashRegisterInfoGraphType> handler;

        public GetCashRegisterByKioskTokenTest()
        {
            handler = new GetCashRegisterByKioskToken(DbContext);
        }

        [Fact]
        public async Task ReturnsInvalidForUnknownToken()
        {
            var result = await handler.Handle(new GetCashRegisterByKioskToken.Input { Token = "unknown" }, CancellationToken.None);

            result.IsValid.Should().BeFalse();
        }

        [Fact]
        public async Task ReturnsValidForActiveKiosk()
        {
            var project = new Project { Name = "Programme test" };
            var market = new Market { Name = "Marché test" };
            var marketGroup = new MarketGroup { Name = "Groupe", Project = project };
            var cashRegister = new CashRegister
            {
                Name = "Caisse 1",
                Market = market,
                KioskAccessToken = KioskHelper.GenerateAccessToken()
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

            var result = await handler.Handle(new GetCashRegisterByKioskToken.Input
            {
                Token = cashRegister.KioskAccessToken
            }, CancellationToken.None);

            result.IsValid.Should().BeTrue();
            result.CashRegisterName.Should().Be("Caisse 1");
            result.MarketIsDisabled.Should().BeFalse();
            result.ProgramNames.Should().ContainSingle().Which.Should().Be("Programme test");
        }

        [Fact]
        public async Task ReturnsDistinctProgramNamesForMultipleMarketGroups()
        {
            var projectA = new Project { Name = "Programme A" };
            var projectB = new Project { Name = "Programme B" };
            var market = new Market { Name = "Marché test" };
            var marketGroupA1 = new MarketGroup { Name = "Groupe A1", Project = projectA };
            var marketGroupA2 = new MarketGroup { Name = "Groupe A2", Project = projectA };
            var marketGroupB = new MarketGroup { Name = "Groupe B", Project = projectB };
            var cashRegister = new CashRegister
            {
                Name = "Caisse multi",
                Market = market,
                KioskAccessToken = KioskHelper.GenerateAccessToken()
            };

            DbContext.Projects.AddRange(projectA, projectB);
            DbContext.Markets.Add(market);
            DbContext.MarketGroups.AddRange(marketGroupA1, marketGroupA2, marketGroupB);
            DbContext.CashRegisters.Add(cashRegister);
            await DbContext.SaveChangesAsync();

            DbContext.CashRegisterMarketGroups.AddRange(
                new CashRegisterMarketGroup { CashRegisterId = cashRegister.Id, MarketGroupId = marketGroupA1.Id },
                new CashRegisterMarketGroup { CashRegisterId = cashRegister.Id, MarketGroupId = marketGroupA2.Id },
                new CashRegisterMarketGroup { CashRegisterId = cashRegister.Id, MarketGroupId = marketGroupB.Id });
            await DbContext.SaveChangesAsync();

            var result = await handler.Handle(new GetCashRegisterByKioskToken.Input
            {
                Token = cashRegister.KioskAccessToken
            }, CancellationToken.None);

            result.IsValid.Should().BeTrue();
            result.ProgramNames.Should().Equal("Programme A", "Programme B");
        }
    }
}
