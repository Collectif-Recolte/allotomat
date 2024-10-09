using Sig.App.Backend.DbModel.Entities.MarketGroups;

namespace Sig.App.Backend.DbModel.Entities.CashRegisters
{
    public class CashRegisterMarketGroup
    {
        public long MarketGroupId { get; set; }
        public long CashRegisterId { get; set; }

        public MarketGroup MarketGroup { get; set; }
        public CashRegister CashRegister { get; set; }
    }
}
