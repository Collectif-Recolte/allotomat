using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Requests.Commands.Mutations.CashRegisters;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Sig.App.Backend.Extensions;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.CashRegisters
{
    public class AddCashRegisterToMarketGroupTest : TestBase
    {
        private readonly AddCashRegisterToMarketGroup handler;
        private readonly MarketGroup marketGroup;
        private readonly CashRegister cashRegister;

        public AddCashRegisterToMarketGroupTest()
        {
            var project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            var market = new Market()
            {
                Name = "Market 1"
            };
            DbContext.Markets.Add(market);

            marketGroup = new MarketGroup()
            {
                Name = "Market group 1",
                Project = project,
                Markets = new List<MarketGroupMarket>()
            };
            DbContext.MarketGroups.Add(marketGroup);

            marketGroup.Markets.Add(new MarketGroupMarket()
            {
                Market = market,
                MarketGroup = marketGroup
            });

            cashRegister = new CashRegister()
            {
                Name = "Cash register 1",
                Market = market
            };
            DbContext.CashRegisters.Add(cashRegister);

            DbContext.SaveChanges();

            handler = new AddCashRegisterToMarketGroup(NullLogger<AddCashRegisterToMarketGroup>.Instance, DbContext);
        }

        [Fact]
        public async Task AddCashRegisterToMarketGroup()
        {
            var input = new AddCashRegisterToMarketGroup.Input()
            {
                MarketGroupId = marketGroup.GetIdentifier(),
                CashRegisterId = cashRegister.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var cashRegisterMarketGroupCount = await DbContext.CashRegisterMarketGroups.CountAsync();
            cashRegisterMarketGroupCount.Should().Be(1);
        }

        [Fact]
        public async Task ThrowsIfMarketGroupNotFoundException()
        {
            var input = new AddCashRegisterToMarketGroup.Input()
            {
                CashRegisterId = cashRegister.GetIdentifier(),
                MarketGroupId = Id.New<MarketGroup>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddCashRegisterToMarketGroup.MarketGroupNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfCashRegisterNotFoundException()
        {
            var input = new AddCashRegisterToMarketGroup.Input()
            {
                CashRegisterId = Id.New<CashRegister>(123456),
                MarketGroupId = marketGroup.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddCashRegisterToMarketGroup.CashRegisterNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfCashRegisterAlreadyInMarketGroupException()
        {
            cashRegister.MarketGroups = new List<CashRegisterMarketGroup>()
            {
                new CashRegisterMarketGroup()
                {
                    CashRegister = cashRegister,
                    MarketGroup = marketGroup
                }
            };
            await DbContext.SaveChangesAsync();

            var input = new AddCashRegisterToMarketGroup.Input()
            {
                CashRegisterId = cashRegister.GetIdentifier(),
                MarketGroupId = marketGroup.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddCashRegisterToMarketGroup.CashRegisterAlreadyInMarketGroupException>();
        }
    }
}