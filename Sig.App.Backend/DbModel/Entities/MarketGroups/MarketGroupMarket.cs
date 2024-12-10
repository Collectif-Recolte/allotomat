using Sig.App.Backend.DbModel.Entities.Markets;

namespace Sig.App.Backend.DbModel.Entities.MarketGroups
{
    public class MarketGroupMarket
    {
        public long MarketGroupId { get; set; }
        public long MarketId { get; set; }

        public MarketGroup MarketGroup { get; set; }
        public Market Market { get; set; }
    }
}
