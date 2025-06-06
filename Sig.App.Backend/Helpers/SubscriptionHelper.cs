﻿using NodaTime;
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
            var needExtraDay = false;

            if (subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth ||
                subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth)
            {
                int monthsApart = 12 * (endDate.Year - startDate.Year) + endDate.Month - startDate.Month;

                cardPaymentRemaining += monthsApart;

                if (startDate > today && startDate.Day == 1) needExtraDay = true;
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

            if (needExtraDay) cardPaymentRemaining++;

            return Math.Max(0, subscription.IsSubscriptionPaymentBasedCardUsage ? Math.Min(cardPaymentRemaining, subscription.MaxNumberOfPayments.Value) : cardPaymentRemaining);
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
                if (startDate.Day == 1) cardPaymentRemaining++;
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

                if (startDate.Day == 15) cardPaymentRemaining++;
            }

            return subscription.IsSubscriptionPaymentBasedCardUsage ? Math.Min(subscription.MaxNumberOfPayments.Value, cardPaymentRemaining) : cardPaymentRemaining;
        }

        public static DateTime GetFirstPaymentDateTime(this Subscription subscription, IClock clock)
        {
            var startDate = subscription.StartDate;

            if (subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth)
            {
                if (startDate.Day == 1)
                {
                    return startDate;
                }
                else
                {
                    return new DateTime(startDate.Year, startDate.Month, 1).AddMonths(1);
                }
            }
            else if (subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth)
            {
                if (startDate.Day <= 15) {
                    return new DateTime(startDate.Year, startDate.Month, 15);
                }
                else
                {
                    return new DateTime(startDate.Year, startDate.Month, 15).AddMonths(1);
                }
            }
            else
            {
                if (startDate.Day == 1)
                {
                    return startDate;
                }
                else if (startDate.Day <= 15)
                {
                    return new DateTime(startDate.Year, startDate.Month, 15);
                }
                else {
                    return new DateTime(startDate.Year, startDate.Month, 1).AddMonths(1);
                }
            }
        }

        public static DateTime GetLastExpirationDateTime(this Subscription subscription, IClock clock)
        {
            var endDate = subscription.EndDate;

            if (subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth)
            {
                if (endDate.Day == 1)
                {
                    return endDate;
                }
                else
                {
                    return new DateTime(endDate.Year, endDate.Month, 1).AddMonths(1);
                }
            }
            else if (subscription.MonthlyPaymentMoment == SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth)
            {
                if (endDate.Day <= 15)
                {
                    return new DateTime(endDate.Year, endDate.Month, 15);
                }
                else
                {
                    return new DateTime(endDate.Year, endDate.Month, 15).AddMonths(1);
                }
            }
            else
            {
                if (endDate.Day == 1)
                {
                    return endDate;
                }
                else if (endDate.Day <= 15)
                {
                    return new DateTime(endDate.Year, endDate.Month, 15);
                }
                else
                {
                    return new DateTime(endDate.Year, endDate.Month, 1).AddMonths(1);
                }
            }
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
