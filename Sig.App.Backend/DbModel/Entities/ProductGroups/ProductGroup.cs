using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.ProductGroups
{
    public class ProductGroup : IHaveLongIdentifier
    {
        public long Id { get; set; }

        public long ProjectId { get; set; }
        public Project Project { get; set; }

        public string Name { get; set; }
        public ProductGroupColor Color { get; set; }
        public int OrderOfAppearance { get; set; }

        public IList<SubscriptionType> Types { get; set; }
        public IList<Fund> Funds { get; set; }
    }
}
