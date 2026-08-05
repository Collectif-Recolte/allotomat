using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.CashRegisters;
using Sig.App.Backend.Requests.Queries.Markets;
using Sig.App.Backend.Utilities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Queries.Markets
{
    public class SearchMarketGroupMarketAmountOwedsTest : TestBase
    {
        private readonly SearchMarketGroupMarketAmountOweds handler;
        private readonly Project project;
        private readonly Market market;
        private readonly MarketGroup marketGroup;
        private readonly MarketGroup otherMarketGroup;
        private readonly CashRegister cashRegister1;
        private readonly CashRegister cashRegister2;

        public SearchMarketGroupMarketAmountOwedsTest()
        {
            project = new Project { Name = "Programme 1" };
            market = new Market { Name = "Commerce 1" };
            marketGroup = new MarketGroup { Name = "Groupe 1", Project = project };
            otherMarketGroup = new MarketGroup { Name = "Groupe 2", Project = project };
            cashRegister1 = new CashRegister { Name = "Caisse 1", Market = market };
            cashRegister2 = new CashRegister { Name = "Caisse 2", Market = market };

            DbContext.Projects.Add(project);
            DbContext.Markets.Add(market);
            DbContext.MarketGroups.AddRange(marketGroup, otherMarketGroup);
            DbContext.CashRegisters.AddRange(cashRegister1, cashRegister2);
            DbContext.SaveChanges();

            DbContext.CashRegisterMarketGroups.AddRange(
                new CashRegisterMarketGroup { CashRegisterId = cashRegister1.Id, MarketGroupId = marketGroup.Id },
                new CashRegisterMarketGroup { CashRegisterId = cashRegister2.Id, MarketGroupId = marketGroup.Id });
            DbContext.SaveChanges();

            handler = new SearchMarketGroupMarketAmountOweds(DbContext);
        }

        [Fact]
        public async Task IncludesArchivedCashRegisterAmounts()
        {
            AddPaymentLog(cashRegister1, marketGroup, 5m);
            AddPaymentLog(cashRegister2, marketGroup, 10m);
            AddPaymentLog(cashRegister2, otherMarketGroup, 99m);
            await DbContext.SaveChangesAsync();

            await Archive(cashRegister2);

            var result = await handler.Handle(QueryFor(marketGroup), CancellationToken.None);

            result.TotalAmount.Should().Be(15m);
            result.Items.Should().ContainSingle();

            var amounts = result.Items.Single().AmountByCashRegister.ToList();
            amounts.Should().HaveCount(2);
            amounts.Should().Contain(x => x.CashRegister.Id == cashRegister1.GetIdentifier() && x.Amount == 5m && !x.CashRegister.IsArchived);
            amounts.Should().Contain(x => x.CashRegister.Id == cashRegister2.GetIdentifier() && x.Amount == 10m && x.CashRegister.IsArchived);
        }

        [Fact]
        public async Task SubtractsRefundsOnArchivedCashRegister()
        {
            AddPaymentLog(cashRegister2, marketGroup, 10m);
            AddRefundLog(cashRegister2, marketGroup, 3m);
            await DbContext.SaveChangesAsync();

            await Archive(cashRegister2);

            var result = await handler.Handle(QueryFor(marketGroup), CancellationToken.None);

            result.TotalAmount.Should().Be(7m);
            result.Items.Single().AmountByCashRegister.Should().ContainSingle()
                .Which.Amount.Should().Be(7m);
        }

        private SearchMarketGroupMarketAmountOweds.Query QueryFor(MarketGroup group)
        {
            return new SearchMarketGroupMarketAmountOweds.Query
            {
                MarketGroupId = group.Id,
                Page = new Page(1, 20),
                StartDate = Clock.GetCurrentInstant().ToDateTimeUtc().AddDays(-1),
                EndDate = Clock.GetCurrentInstant().ToDateTimeUtc().AddDays(1)
            };
        }

        private void AddPaymentLog(CashRegister cashRegister, MarketGroup group, decimal amount)
        {
            DbContext.TransactionLogs.Add(new TransactionLog
            {
                TransactionUniqueId = Guid.NewGuid().ToString(),
                CreatedAtUtc = Clock.GetCurrentInstant().ToDateTimeUtc(),
                Discriminator = TransactionLogDiscriminator.PaymentTransactionLog,
                TotalAmount = amount,
                MarketId = market.Id,
                MarketName = market.Name,
                CashRegisterId = cashRegister.Id,
                CashRegisterName = cashRegister.Name,
                MarketGroupId = group.Id,
                MarketGroupName = group.Name,
                ProjectId = project.Id,
                ProjectName = project.Name
            });
        }

        private void AddRefundLog(CashRegister cashRegister, MarketGroup group, decimal amount)
        {
            DbContext.TransactionLogs.Add(new TransactionLog
            {
                TransactionUniqueId = Guid.NewGuid().ToString(),
                CreatedAtUtc = Clock.GetCurrentInstant().ToDateTimeUtc(),
                Discriminator = TransactionLogDiscriminator.RefundPaymentTransactionLog,
                TotalAmount = amount,
                MarketId = market.Id,
                MarketName = market.Name,
                CashRegisterId = cashRegister.Id,
                CashRegisterName = cashRegister.Name,
                MarketGroupId = group.Id,
                MarketGroupName = group.Name,
                ProjectId = project.Id,
                ProjectName = project.Name
            });
        }

        private async Task Archive(CashRegister cashRegister)
        {
            var archiveHandler = new ArchiveCashRegister(NullLogger<ArchiveCashRegister>.Instance, DbContext);
            await archiveHandler.Handle(new ArchiveCashRegister.Input
            {
                CashRegisterId = cashRegister.GetIdentifier()
            }, CancellationToken.None);
        }
    }
}
