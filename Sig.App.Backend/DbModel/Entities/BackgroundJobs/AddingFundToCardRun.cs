using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.BackgroundJobs
{
    public class AddingFundToCardRun
    {
        public long Id { get; set; }

        public string Name { get; set; }
        public IList<SubscriptionMonthlyPaymentMoment> Moments { get; set; }
        public DateTime Date { get; set; }
    }
}
