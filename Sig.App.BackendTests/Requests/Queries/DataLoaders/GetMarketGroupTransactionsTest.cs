using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Requests.Commands.Mutations.CashRegisters;
using Sig.App.Backend.Requests.Queries.DataLoaders;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Queries.DataLoaders
{
    public class GetMarketGroupTransactionsTest : TestBase
    {
        private readonly GetMarketGroupTransactions handler;
        private readonly Project project;
        private readonly Market market;
        private readonly MarketGroup marketGroup;
        private readonly MarketGroup otherMarketGroup;
        private readonly CashRegister cashRegister1;
        private readonly CashRegister cashRegister2;

        public GetMarketGroupTransactionsTest()
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

            handler = new GetMarketGroupTransactions(DbContext);
        }

        [Fact]
        public async Task IncludesArchivedCashRegisterTransactions()
        {
            var payment1 = AddPayment(cashRegister1, marketGroup, 5m);
            var payment2 = AddPayment(cashRegister2, marketGroup, 10m);
            AddPayment(cashRegister2, otherMarketGroup, 99m);
            await DbContext.SaveChangesAsync();

            await Archive(cashRegister2);

            var result = await handler.Handle(QueryFor(marketGroup), CancellationToken.None);
            var transactions = result[marketGroup.Id].ToList();

            transactions.Should().HaveCount(2);
            transactions.Should().Contain(x => x.Id == payment1.GetIdentifier() && x.Amount == 5m);
            transactions.Should().Contain(x => x.Id == payment2.GetIdentifier() && x.Amount == 10m);
        }

        [Fact]
        public async Task IncludesRefundsFromArchivedCashRegister()
        {
            var payment = AddPayment(cashRegister2, marketGroup, 10m);
            var refund = AddRefund(cashRegister2, marketGroup, payment, 3m);
            await DbContext.SaveChangesAsync();

            await Archive(cashRegister2);

            var result = await handler.Handle(QueryFor(marketGroup), CancellationToken.None);
            var transactions = result[marketGroup.Id].ToList();

            transactions.Should().HaveCount(2);
            transactions.Should().Contain(x => x.Id == payment.GetIdentifier() && x.Amount == 10m);
            transactions.Should().Contain(x => x.Id == refund.GetIdentifier() && x.Amount == 3m);
            transactions.Should().Contain(x => x is RefundTransactionGraphType);
        }

        [Fact]
        public async Task FiltersByCashRegisterIds()
        {
            var payment1 = AddPayment(cashRegister1, marketGroup, 5m);
            AddPayment(cashRegister2, marketGroup, 10m);
            await DbContext.SaveChangesAsync();

            var result = await handler.Handle(QueryFor(marketGroup, cashRegister1.Id), CancellationToken.None);
            var transactions = result[marketGroup.Id].ToList();

            transactions.Should().ContainSingle()
                .Which.Id.Should().Be(payment1.GetIdentifier());
        }

        private GetMarketGroupTransactions.Query QueryFor(MarketGroup group, params long[] cashRegisterIds)
        {
            return new GetMarketGroupTransactions.Query
            {
                Ids = new[] { group.Id },
                Group = new TransactionFilter(
                    Clock.GetCurrentInstant().Minus(Duration.FromDays(1)),
                    Clock.GetCurrentInstant().Plus(Duration.FromDays(1)),
                    cashRegisterIds)
            };
        }

        private PaymentTransaction AddPayment(CashRegister cashRegister, MarketGroup group, decimal amount)
        {
            var uniqueId = Guid.NewGuid().ToString();
            var createdAt = Clock.GetCurrentInstant().ToDateTimeUtc();
            var payment = new PaymentTransaction
            {
                TransactionUniqueId = uniqueId,
                Amount = amount,
                CreatedAtUtc = createdAt,
                MarketId = market.Id,
                CashRegisterId = cashRegister.Id
            };

            DbContext.Transactions.Add(payment);
            DbContext.TransactionLogs.Add(new TransactionLog
            {
                TransactionUniqueId = uniqueId,
                CreatedAtUtc = createdAt,
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

            return payment;
        }

        private RefundTransaction AddRefund(CashRegister cashRegister, MarketGroup group, PaymentTransaction initialPayment, decimal amount)
        {
            var uniqueId = Guid.NewGuid().ToString();
            var createdAt = Clock.GetCurrentInstant().ToDateTimeUtc();
            var refund = new RefundTransaction
            {
                TransactionUniqueId = uniqueId,
                Amount = amount,
                CreatedAtUtc = createdAt,
                CashRegisterId = cashRegister.Id,
                InitialTransaction = initialPayment
            };

            DbContext.Transactions.Add(refund);
            DbContext.TransactionLogs.Add(new TransactionLog
            {
                TransactionUniqueId = uniqueId,
                CreatedAtUtc = createdAt,
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

            return refund;
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
