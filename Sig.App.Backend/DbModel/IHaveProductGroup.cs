using Sig.App.Backend.DbModel.Entities.ProductGroups;

namespace Sig.App.Backend.DbModel
{
    public interface IHaveProductGroup
    {
        long ProductGroupId { get; set; }
        ProductGroup ProductGroup { get; set; }
    }
}
