using Sig.App.Backend.DbModel.Entities.CashRegisters;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class CashRegisterAmountOwedGraphType
    {
        public CashRegisterGraphType CashRegister { get; set; }
        public decimal Amount { get; set; }
    }
}
