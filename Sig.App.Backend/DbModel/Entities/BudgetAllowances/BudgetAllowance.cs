using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.BudgetAllowances
{
    public class BudgetAllowance : IHaveLongIdentifier
    {
        public long Id { get; set; }

        public long OrganizationId { get; set; }
        public Organization Organization { get; set; }

        public long SubscriptionId { get; set; }
        public Subscription Subscription { get; set; }

        public decimal OriginalFund { get; set; }
        public decimal AvailableFund { get; set; }

        public IList<SubscriptionBeneficiary> Beneficiaries { get; set; }
    }
}
