using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Requests.Commands.Mutations.CashRegisters;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.CashRegisters
{
    public class EditCashRegisterTest : TestBase
    {
        private readonly EditCashRegister handler;
        private readonly CashRegister cashRegister;
        public EditCashRegisterTest()
        {
            cashRegister = new CashRegister()
            {
                Name = "Caisse 1"
            };
            DbContext.CashRegisters.Add(cashRegister);

            DbContext.SaveChanges();

            handler = new EditCashRegister(NullLogger<EditCashRegister>.Instance, DbContext);
        }

        [Fact]
        public async Task EditTheCashRegister()
        {
            var input = new EditCashRegister.Input()
            {
                CashRegisterId = cashRegister.GetIdentifier(),
                Name = new Maybe<NonNull<string>>("Caisse 1 test"),
            };

            await handler.Handle(input, CancellationToken.None);

            var localCashRegister = await DbContext.CashRegisters.FirstAsync();

            localCashRegister.Name.Should().Be("Caisse 1 test");
        }

        [Fact]
        public async Task ThrowsIfCashRegisterNotFound()
        {
            var input = new EditCashRegister.Input()
            {
                CashRegisterId = Id.New<CashRegister>(123456),
                Name = new Maybe<NonNull<string>>("Caisse 1 test")
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditCashRegister.CashRegisterNotFoundException>();
        }
    }
}
