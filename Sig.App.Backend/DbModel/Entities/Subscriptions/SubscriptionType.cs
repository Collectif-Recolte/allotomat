﻿using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.ProductGroups;

namespace Sig.App.Backend.DbModel.Entities.Subscriptions
{
    public class SubscriptionType : IHaveLongIdentifier
    {
        public long Id { get; set; }

        public long SubscriptionId { get; set; }
        public Subscription Subscription { get; set; }

        public long? BeneficiaryTypeId { get; set; }
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        public BeneficiaryType? BeneficiaryType { get; set; }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        public long ProductGroupId { get; set; }
        public ProductGroup ProductGroup { get; set; }

        public decimal Amount { get; set; }
    }
}
