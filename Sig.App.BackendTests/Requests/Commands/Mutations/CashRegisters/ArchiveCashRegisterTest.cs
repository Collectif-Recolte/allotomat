using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.CashRegisters;
using Sig.App.Backend.Services.Mailer;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.CashRegisters
{
    public class ArchiveCashRegisterTest : TestBase
    {
        private readonly IRequestHandler<ArchiveCashRegister.Input> handler;
        private Mock<IMailer> mailer;
        private readonly CashRegister cashRegister;

        public ArchiveCashRegisterTest()
        {
            cashRegister = new CashRegister()
            {
                Name = "Caisse 1"
            };
            DbContext.CashRegisters.Add(cashRegister);

            DbContext.SaveChanges();

            handler = new ArchiveCashRegister(NullLogger<ArchiveCashRegister>.Instance, DbContext);
        }

        [Fact]
        public async Task ArchiveTheCashRegister()
        {
            var input = new ArchiveCashRegister.Input()
            {
                CashRegisterId = cashRegister.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var cashRegisterCount = await DbContext.CashRegisters.CountAsync();
            cashRegisterCount.Should().Be(1);

            var localCashRegister = await DbContext.CashRegisters.FirstAsync();
            localCashRegister.IsArchived.Should().BeTrue();
        }

        [Fact]
        public async Task ThrowsIfCashRegisterNotFound()
        {
            var input = new ArchiveCashRegister.Input()
            {
                CashRegisterId = Id.New<CashRegister>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ArchiveCashRegister.CashRegisterNotFoundException>();
        }
    }
}
