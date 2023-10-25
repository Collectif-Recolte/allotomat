using Sig.App.Backend.DbModel.Entities.ProductGroups;

namespace Sig.App.Backend.DbModel.Entities.Cards
{
    public class Fund : IHaveLongIdentifier
    {
        public long Id { get; set; }

        public long? CardId { get; set; }
        public Card Card { get; set; }

        public long ProductGroupId { get; set; }
        public ProductGroup ProductGroup { get; set; }

        public decimal Amount { get; set; }
    }
}
