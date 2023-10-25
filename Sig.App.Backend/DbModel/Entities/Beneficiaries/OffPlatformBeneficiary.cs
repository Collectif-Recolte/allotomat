using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.Beneficiaries
{
    public class OffPlatformBeneficiary : Beneficiary
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public SubscriptionMonthlyPaymentMoment? MonthlyPaymentMoment { get; set; }

        public bool IsActive { get; set; }

        public IList<PaymentFund> PaymentFunds { get; set; }
    }
}
