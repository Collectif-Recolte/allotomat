using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.CashRegisters;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.CashRegisters
{
    public class CreateCashRegisterTest : TestBase
    {
        private readonly CreateCashRegister handler;
        private readonly Market market;
        private readonly MarketGroup marketGroup;
        private readonly Project project;

        public CreateCashRegisterTest()
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

            DbContext.SaveChanges();

            handler = new CreateCashRegister(NullLogger<CreateCashRegister>.Instance, DbContext);
        }

        [Fact]
        public async Task CreateTheCashRegister()
        {
            var input = new CreateCashRegister.Input()
            {
                Name = "Cash register Test 1",
                MarketId = market.GetIdentifier(),
                MarketGroupId = marketGroup.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var cashRegister = await DbContext.CashRegisters.Include(x => x.MarketGroups).FirstAsync();

            cashRegister.Name.Should().Be("Cash register Test 1");
            cashRegister.Market.Name.Should().Be("Market 1");
            cashRegister.MarketGroups.Count.Should().Be(1);
        }

        [Fact]
        public async Task ThrowsIfMarketNotFound()
        {
            var input = new CreateCashRegister.Input()
            {
                Name = "Cash register Test 1",
                MarketId = Id.New<Market>(123456),
                MarketGroupId = marketGroup.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateCashRegister.MarketNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfMarketGroupNotFound()
        {
            var input = new CreateCashRegister.Input()
            {
                Name = "Cash register Test 1",
                MarketId = market.GetIdentifier(),
                MarketGroupId = Id.New<MarketGroup>(123456),
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateCashRegister.MarketGroupNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfMarketGroupNotFoundInMarket()
        {
            var marketGroup2 = new MarketGroup()
            {
                Name = "Market group 2",
                Project = project,
                Markets = new List<MarketGroupMarket>()
            };
            DbContext.MarketGroups.Add(marketGroup2);
            await DbContext.SaveChangesAsync();

            var input = new CreateCashRegister.Input()
            {
                Name = "Cash register Test 1",
                MarketId = market.GetIdentifier(),
                MarketGroupId = Id.New<MarketGroup>(123456),
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateCashRegister.MarketGroupNotFoundException>();
        }
    }
}