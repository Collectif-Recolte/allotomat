using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.ProductGroups;

namespace Sig.App.Backend.DbModel.Entities.Subscriptions
{
    public class SubscriptionType : IHaveLongIdentifier
    {
        public long Id { get; set; }

        public long SubscriptionId { get; set; }
        public Subscription Subscription { get; set; }

        public long? BeneficiaryTypeId { get; set; }
        public BeneficiaryType? BeneficiaryType { get; set; }

        public long ProductGroupId { get; set; }
        public ProductGroup ProductGroup { get; set; }

        public decimal Amount { get; set; }
    }
}
