using NodaTime;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using System;

namespace Sig.App.Backend.Helpers
{
    public static class SubscriptionHelper
    {
        public static int GetEffectiveMaxNumberOfPayments(this SubscriptionBeneficiary subscriptionBeneficiary)
        {
            return subscriptionBeneficiary.MaxNumberOfPaymentsOverride
                ?? subscriptionBeneficiary.Subscription.MaxNumberOfPayments
                ?? subscriptionBeneficiary.GetTotalPayment();
        }

        public static int GetPaymentRemaining(this SubscriptionBeneficiary subscriptionBeneficiary, IClock clock)
        {
            var cardPaymentRemaining = GetCardPaymentRemaining(subscriptionBeneficiary.Subscription, clock);
            if (subscriptionBeneficiary.Subscription.IsSubscriptionPaymentBasedCardUsage)
            {
                cardPaymentRemaining = Math.Min(cardPaymentRemaining, subscriptionBeneficiary.GetEffectiveMaxNumberOfPayments());
            }
            return Math.Max(0, cardPaymentRemaining);
        }

        public static int GetPaymentRemaining(this Subscription subscription, IClock clock)
        {
            var cardPaymentRemaining = GetCardPaymentRemaining(subscription, clock);
            return Math.Max(0, subscription.IsSubscriptionPaymentBasedCardUsage ? Math.Min(cardPaymentRemaining, subscription.MaxNumberOfPayments.Value) : cardPaymentRemaining);
        }

        public static int GetCardPaymentRemaining(this Subscription subscription, IClock clock)
        {
            var cardPaymentRemaining = 0;
            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            var startDate = today > subscription.StartDate ? today : subscription.StartDate;
            var endDate = subscription.EndDate;
            var needExtraDay = false;

            if (subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth ||
                subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth)
            {
                cardPaymentRemaining += MonthsBetween(startDate, endDate);
                if (startDate > today && startDate.Day == 1) needExtraDay = true;
            }

            if (subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth ||
                subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth)
            {
                cardPaymentRemaining += AdjustedMonthsForFifteenth(startDate, endDate);
            }

            if (needExtraDay) cardPaymentRemaining++;

            return cardPaymentRemaining;
        }

        public static int GetTotalPayment(this Subscription subscription)
        {
            var totalPayment = GetTotalPaymentBySubscription(subscription);
            return subscription.IsSubscriptionPaymentBasedCardUsage ? Math.Min(subscription.MaxNumberOfPayments.Value, totalPayment) : totalPayment;
        }

        public static int GetTotalPayment(this SubscriptionBeneficiary subscriptionBeneficiary)
        {
            var totalPayment = GetTotalPaymentBySubscription(subscriptionBeneficiary.Subscription);
            return subscriptionBeneficiary.Subscription.IsSubscriptionPaymentBasedCardUsage ? Math.Min(subscriptionBeneficiary.GetEffectiveMaxNumberOfPayments(), totalPayment) : totalPayment;
        }

        public static int GetPreviousPaymentCount(this Subscription subscription, IClock clock)
        {
            var today = clock.GetCurrentInstant().ToDateTimeUtc();
            return Math.Max(0, CountPaymentsSinceStart(subscription, today));
        }

        private static int GetTotalPaymentBySubscription(Subscription subscription)
        {
            return CountPaymentsSinceStart(subscription, subscription.EndDate);
        }

        private static int CountPaymentsSinceStart(Subscription subscription, DateTime to)
        {
            var count = 0;
            var from = subscription.StartDate;

            if (subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth ||
                subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth)
            {
                count += MonthsBetween(from, to);
                if (from.Day == 1) count++;
            }

            if (subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth ||
                subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth)
            {
                count += AdjustedMonthsForFifteenth(from, to);
                if (from.Day == 15) count++;
            }

            return count;
        }

        private static int MonthsBetween(DateTime from, DateTime to) =>
            12 * (to.Year - from.Year) + to.Month - from.Month;

        private static int AdjustedMonthsForFifteenth(DateTime from, DateTime to)
        {
            var months = MonthsBetween(from, to);
            if (from.Day < 15 && to.Day >= 15) months++;
            if (from.Day >= 15 && to.Day < 15) months--;
            return months;
        }

        public static DateTime GetFirstPaymentDateTime(this Subscription subscription) =>
            NextPaymentDateOnOrAfter(subscription.StartDate, subscription.MonthlyPaymentMoment);

        public static DateTime GetLastExpirationDateTime(this Subscription subscription) =>
            NextPaymentDateOnOrAfter(subscription.EndDate, subscription.MonthlyPaymentMoment);

        private static DateTime NextPaymentDateOnOrAfter(DateTime date, SubscriptionMonthlyPaymentMoment moment)
        {
            if (moment == SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth)
                return date.Day == 1 ? date : new DateTime(date.Year, date.Month, 1).AddMonths(1);

            if (moment == SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth)
                return date.Day <= 15 ? new DateTime(date.Year, date.Month, 15) : new DateTime(date.Year, date.Month, 15).AddMonths(1);

            // FirstAndFifteenthDayOfTheMonth
            if (date.Day == 1) return date;
            if (date.Day <= 15) return new DateTime(date.Year, date.Month, 15);
            return new DateTime(date.Year, date.Month, 1).AddMonths(1);
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

        public static DateTime GetPreviousPaymentDateTime(IClock clock, SubscriptionMonthlyPaymentMoment moment)
        {
            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            if (moment == SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth)
            {
                return new DateTime(today.Year, today.Month, 1).AddMonths(-1);
            }
            else if (moment == SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth)
            {
                return new DateTime(today.Year, today.Month, 15).AddMonths(-1);
            }
            else
            {
                if (today.Day >= 15)
                {
                    return new DateTime(today.Year, today.Month, 1);
                }
                else
                {
                    return new DateTime(today.Year, today.Month, 15).AddMonths(-1);
                }
            }
        }
    }
}
