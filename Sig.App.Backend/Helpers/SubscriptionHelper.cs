using NodaTime;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using System;

namespace Sig.App.Backend.Helpers
{
    public static class SubscriptionHelper
    {
        public static int GetPaymentRemaining(this Subscription subscription, IClock clock)
        {
            var cardPaymentRemaining = 0;
            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            var startDate = today > subscription.StartDate ? today : subscription.StartDate;
            var endDate = subscription.EndDate;

            if (subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth ||
                subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth)
            {
                int monthsApart = 12 * (endDate.Year - startDate.Year) + endDate.Month - startDate.Month;

                cardPaymentRemaining += monthsApart;

                if (startDate > today && startDate.Day == 1) cardPaymentRemaining++;
            }

            if (subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth ||
                subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth)
            {
                int monthsApart = 12 * (endDate.Year - startDate.Year) + endDate.Month - startDate.Month;
                if (today.Day < 15 && endDate.Day >= 15)
                {
                    monthsApart++;
                }
                if (today.Day >= 15 && endDate.Day < 15)
                {
                    monthsApart--;
                }

                cardPaymentRemaining += monthsApart;

                if (startDate > today && startDate.Day <= 15) cardPaymentRemaining++;
            }

            return Math.Max(0, cardPaymentRemaining);
        }

        public static int GetTotalPayment(this Subscription subscription)
        {
            var cardPaymentRemaining = 0;
            
            var startDate = subscription.StartDate;
            var endDate = subscription.EndDate;

            if (subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth ||
                subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth)
            {
                int monthsApart = 12 * (endDate.Year - startDate.Year) + endDate.Month - startDate.Month;

                cardPaymentRemaining += monthsApart;
            }

            if (subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth ||
                subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth)
            {
                int monthsApart = 12 * (endDate.Year - startDate.Year) + endDate.Month - startDate.Month;
                if (startDate.Day < 15 && endDate.Day >= 15)
                {
                    monthsApart++;
                }
                if (startDate.Day >= 15 && endDate.Day < 15)
                {
                    monthsApart--;
                }

                cardPaymentRemaining += monthsApart;
            }

            return cardPaymentRemaining;
        }

        public static DateTime GetNextPaymentDateTime(IClock clock, SubscriptionMonthlyPaymentMoment moment)
        {
            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            if (moment == SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth)
            {
                return new DateTime(today.Year, today.Month, 1).AddMonths(1);
            }
            else if (moment == SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth)
            {
                return new DateTime(today.Year, today.Month, 15).AddMonths(1);
            }
            else
            {
                if (today.Day >= 15)
                {
                    return new DateTime(today.Year, today.Month, 1).AddMonths(1);
                }
                else
                {
                    return new DateTime(today.Year, today.Month, 15);
                }
            }
        }
    }
}
