using Sig.App.Backend.DbModel.Entities.Markets;

namespace Sig.App.Backend.DbModel.Entities.Organizations
{
    public class OrganizationMarket
    {
        public long OrganizationId { get; set; }
        public long MarketId { get; set; }

        public Organization Organization { get; set; }
        public Market Market { get; set; }
    }
}
