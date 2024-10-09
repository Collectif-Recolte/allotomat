using Sig.App.Backend.DbModel.Entities.Markets;
using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.CashRegisters
{
    public class CashRegister : IHaveLongIdentifier
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public long MarketId { get; set; }
        public Market Market { get; set; }

        public IList<CashRegisterMarketGroup> MarketGroups { get; set; }

        public bool IsArchived { get; set; }
    }
}
