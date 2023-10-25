using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;

namespace Sig.App.Backend.DbModel.Entities.Subscriptions
{
    public class SubscriptionBeneficiary
    {
        public long SubscriptionId { get; set; }
        public long BeneficiaryId { get; set; }
        public long? BeneficiaryTypeId { get; set; }
        public long? BudgetAllowanceId { get; set; }

        public Subscription Subscription { get; set; }
        public Beneficiary Beneficiary { get; set; }
        public BeneficiaryType BeneficiaryType { get; set; }
        public BudgetAllowance BudgetAllowance { get; set; }
    }
}
