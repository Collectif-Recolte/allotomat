using Sig.App.Backend.DbModel.Entities.ProductGroups;

namespace Sig.App.Backend.DbModel.Entities.Beneficiaries
{
    public class PaymentFund : IHaveLongIdentifier
    {
        public long Id { get; set; }

        public long BeneficiaryId { get; set; }
        public OffPlatformBeneficiary Beneficiary { get; set; }

        public long ProductGroupId { get; set; }
        public ProductGroup ProductGroup { get; set; }

        public decimal Amount { get; set; }
    }
}
