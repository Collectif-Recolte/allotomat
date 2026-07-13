using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.CashRegisters;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.CashRegisters
{
    public class EnableCashRegisterKioskTest : TestBase
    {
        private readonly IRequestHandler<EnableCashRegisterKiosk.Input, EnableCashRegisterKiosk.Payload> handler;
        private readonly CashRegister cashRegister;

        public EnableCashRegisterKioskTest()
        {
            cashRegister = new CashRegister { Name = "Caisse kiosque" };
            DbContext.CashRegisters.Add(cashRegister);
            DbContext.SaveChanges();

            handler = new EnableCashRegisterKiosk(NullLogger<EnableCashRegisterKiosk>.Instance, DbContext);
        }

        [Fact]
        public async Task EnablesKioskAndGeneratesToken()
        {
            var result = await handler.Handle(new EnableCashRegisterKiosk.Input
            {
                CashRegisterId = cashRegister.GetIdentifier()
            }, CancellationToken.None);

            result.CashRegister.IsKioskEnabled.Should().BeTrue();

            var stored = await DbContext.CashRegisters.FirstAsync();
            stored.KioskAccessToken.Should().NotBeNullOrEmpty();
            stored.KioskPassword.Should().NotBeNullOrEmpty();
            stored.KioskPassword.Length.Should().Be(8);
        }

        [Fact]
        public async Task ThrowsIfAlreadyEnabled()
        {
            await handler.Handle(new EnableCashRegisterKiosk.Input
            {
                CashRegisterId = cashRegister.GetIdentifier()
            }, CancellationToken.None);

            await F(() => handler.Handle(new EnableCashRegisterKiosk.Input
            {
                CashRegisterId = cashRegister.GetIdentifier()
            }, CancellationToken.None))
                .Should().ThrowAsync<EnableCashRegisterKiosk.KioskAlreadyEnabledException>();
        }

        [Fact]
        public async Task ThrowsIfCashRegisterNotFound()
        {
            await F(() => handler.Handle(new EnableCashRegisterKiosk.Input
            {
                CashRegisterId = Id.New<CashRegister>(999999)
            }, CancellationToken.None))
                .Should().ThrowAsync<EnableCashRegisterKiosk.CashRegisterNotFoundException>();
        }
    }
}
