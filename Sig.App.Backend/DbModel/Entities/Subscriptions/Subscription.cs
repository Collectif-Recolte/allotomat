using NodaTime;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Helpers;
using System;
using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.Subscriptions
{
    public class Subscription : IHaveLongIdentifier
    {
        public long Id { get; set; }

        public long ProjectId { get; set; }
        public Project Project { get; set; }

        public string Name { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? FundsExpirationDate { get; set; }
        public SubscriptionMonthlyPaymentMoment MonthlyPaymentMoment { get; set; }
        public bool IsFundsAccumulable { get; set; }

        public IList<SubscriptionType> Types { get; set; }
        public IList<SubscriptionBeneficiary> Beneficiaries { get; set; }
        public IList<BudgetAllowance> BudgetAllowances { get; set; }

        public DateTime GetExpirationDate(IClock clock, SubscriptionMonthlyPaymentMoment moment)
        {
            if (IsFundsAccumulable && FundsExpirationDate.HasValue)
            {
                return FundsExpirationDate.Value;
            }
            else
            {
                return SubscriptionHelper.GetNextPaymentDateTime(clock, moment);
            }
        }
        
        public DateTime GetLastDateToAssignBeneficiary()
        {
            if (IsFundsAccumulable && FundsExpirationDate.HasValue)
            {
                return FundsExpirationDate.Value;
            }

            switch (MonthlyPaymentMoment)
            {
                case SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth:
                case SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth:
                    return EndDate.AddMonths(1).AddDays(-1);
                case SubscriptionMonthlyPaymentMoment.FirstDayOfTheWeek:
                    return EndDate.AddDays(6);
                case SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth:
                    if (EndDate.Day <= 14)
                    {
                        return EndDate.AddDays(13);
                    }
                    
                    var daysInMonth = DateTime.DaysInMonth(EndDate.Year, EndDate.Month);
                    return EndDate.AddDays(daysInMonth - 15);
                default:
                    throw new Exception("Unsupported SubscriptionMonthlyPaymentMoment");
            }
        }
    }
}
