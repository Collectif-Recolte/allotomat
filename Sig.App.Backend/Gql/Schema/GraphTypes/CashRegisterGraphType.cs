using GraphQL.Conventions;
using GraphQL.DataLoader;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class CashRegisterGraphType
    {
        private readonly CashRegister cashRegister;

        public Id Id => cashRegister.GetIdentifier();
        public NonNull<string> Name => cashRegister.Name;

        public CashRegisterGraphType(CashRegister cashRegister)
        {
            this.cashRegister = cashRegister;
        }

        public IDataLoaderResult<MarketGraphType> Market(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadMarket(cashRegister.MarketId);
        }
    }
}
