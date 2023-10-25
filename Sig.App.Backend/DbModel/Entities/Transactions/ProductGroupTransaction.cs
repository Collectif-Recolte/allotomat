using Sig.App.Backend.DbModel.Entities.ProductGroups;

namespace Sig.App.Backend.DbModel.Entities.Transactions
{
    public class ProductGroupTransaction : Transaction, IHaveProductGroup
    {
        public long ProductGroupId { get; set; }
        public ProductGroup ProductGroup { get; set; }
    }
}
