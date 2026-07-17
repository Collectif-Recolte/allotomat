using Microsoft.EntityFrameworkCore;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.BackgroundJobs;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Helpers
{
    public static class SubscriptionHelper
    {
        public const string AddingFundToCardFirstDayOfTheMonthJobName = "AddingFundToCard:FirstDayOfTheMonth";
        public const string AddingFundToCardFifteenthDayOfTheMonthJobName = "AddingFundToCard:FifteenthDayOfTheMonth";
        public const string AddingFundToCardFirstDayOfTheWeekJobName = "AddingFundToCard:FirstDayOfTheWeek";

        public static int GetEffectiveMaxNumberOfPayments(this SubscriptionBeneficiary subscriptionBeneficiary)
        {
            return subscriptionBeneficiary.MaxNumberOfPaymentsOverride
                ?? subscriptionBeneficiary.Subscription.MaxNumberOfPayments
                ?? subscriptionBeneficiary.GetTotalPayment();
        }

        // Nombre de transactions générées par cycle de versement pour un type de bénéficiaire
        // (une SubscriptionAddingFundTransaction par SubscriptionType / ProductGroup).
        public static int GetNumberOfPaymentTypes(this Subscription subscription, long? beneficiaryTypeId)
            => subscription.Types.Count(x => x.BeneficiaryTypeId == beneficiaryTypeId);

        // Convertit un compte brut de transactions en nombre de versements réels.
        public static int GetNumberOfPaymentsMade(int subscriptionTransactionCount, int numberOfPaymentTypes)
            => numberOfPaymentTypes <= 0 ? 0 : subscriptionTransactionCount / numberOfPaymentTypes;

        // Max de versements EXPLICITEMENT configuré (sans fallback sur le total programmé).
        // null => aucune limite de versement manuel.
        public static int? GetExplicitMaxNumberOfPayments(this SubscriptionBeneficiary subscriptionBeneficiary)
            => subscriptionBeneficiary.MaxNumberOfPaymentsOverride
                ?? subscriptionBeneficiary.Subscription.MaxNumberOfPayments;

        public static int GetPaymentRemaining(this SubscriptionBeneficiary subscriptionBeneficiary, IClock clock, bool todaysFundJobCompleted)
        {
            var cardPaymentRemaining = GetCardPaymentRemaining(subscriptionBeneficiary.Subscription, clock, todaysFundJobCompleted);
            if (subscriptionBeneficiary.Subscription.IsSubscriptionPaymentBasedCardUsage)
            {
                cardPaymentRemaining = Math.Min(cardPaymentRemaining, subscriptionBeneficiary.GetEffectiveMaxNumberOfPayments());
            }
            return Math.Max(0, cardPaymentRemaining);
        }

        public static async Task<int> GetPaymentRemainingAsync(
            this SubscriptionBeneficiary subscriptionBeneficiary,
            AppDbContext db,
            IClock clock,
            CancellationToken cancellationToken = default)
        {
            var todaysFundRuns = await GetTodaysAddingFundToCardRunsAsync(db, clock, cancellationToken);
            var todaysFundJobCompleted = IsTodaysFundJobCompleted(subscriptionBeneficiary.Subscription, clock, todaysFundRuns);
            return subscriptionBeneficiary.GetPaymentRemaining(clock, todaysFundJobCompleted);
        }

        public static int GetPaymentRemaining(this Subscription subscription, IClock clock, bool todaysFundJobCompleted)
        {
            var cardPaymentRemaining = GetCardPaymentRemaining(subscription, clock, todaysFundJobCompleted);
            return Math.Max(0, subscription.IsSubscriptionPaymentBasedCardUsage ? Math.Min(cardPaymentRemaining, subscription.MaxNumberOfPayments.Value) : cardPaymentRemaining);
        }

        public static async Task<int> GetPaymentRemainingAsync(
            this Subscription subscription,
            AppDbContext db,
            IClock clock,
            CancellationToken cancellationToken = default)
        {
            var todaysFundRuns = await GetTodaysAddingFundToCardRunsAsync(db, clock, cancellationToken);
            var todaysFundJobCompleted = IsTodaysFundJobCompleted(subscription, clock, todaysFundRuns);
            return subscription.GetPaymentRemaining(clock, todaysFundJobCompleted);
        }

        public static int GetCardPaymentRemaining(this Subscription subscription, IClock clock, bool todaysFundJobCompleted)
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
                if (startDate > today && startDate.Day == 15) needExtraDay = true;
            }

            if (needExtraDay) cardPaymentRemaining++;

            // CRCL-2577: between 00:00 UTC and the AddingFundToCard run on a payment day,
            // the calendar already excludes today's payment, but the job has not delivered it yet.
            if (!todaysFundJobCompleted
                && IsMonthlyPaymentDay(subscription.MonthlyPaymentMoment, today)
                && today >= subscription.StartDate
                && today <= subscription.EndDate)
            {
                cardPaymentRemaining++;
            }

            return cardPaymentRemaining;
        }

        public static async Task<int> GetCardPaymentRemainingAsync(
            this Subscription subscription,
            AppDbContext db,
            IClock clock,
            CancellationToken cancellationToken = default)
        {
            var todaysFundRuns = await GetTodaysAddingFundToCardRunsAsync(db, clock, cancellationToken);
            var todaysFundJobCompleted = IsTodaysFundJobCompleted(subscription, clock, todaysFundRuns);
            return subscription.GetCardPaymentRemaining(clock, todaysFundJobCompleted);
        }

        public static async Task<IReadOnlyList<AddingFundToCardRun>> GetTodaysAddingFundToCardRunsAsync(
            AppDbContext db,
            IClock clock,
            CancellationToken cancellationToken = default)
        {
            var today = clock.GetCurrentInstant().ToDateTimeUtc();
            return await db.AddingFundToCardRuns
                .Where(x => x.Date.Year == today.Year && x.Date.Month == today.Month && x.Date.Day == today.Day)
                .ToListAsync(cancellationToken);
        }

        public static bool IsTodaysFundJobCompleted(
            SubscriptionMonthlyPaymentMoment moment,
            DateTime utcToday,
            IEnumerable<AddingFundToCardRun> todaysRuns)
        {
            if (!IsMonthlyPaymentDay(moment, utcToday) && moment != SubscriptionMonthlyPaymentMoment.FirstDayOfTheWeek)
            {
                return true;
            }

            if (moment == SubscriptionMonthlyPaymentMoment.FirstDayOfTheWeek)
            {
                if (utcToday.DayOfWeek != DayOfWeek.Monday) return true;
                return todaysRuns.Any(x => x.Name == AddingFundToCardFirstDayOfTheWeekJobName);
            }

            var jobName = GetAddingFundToCardJobNameForPaymentDay(moment, utcToday);
            return todaysRuns.Any(x => x.Name == jobName);
        }

        public static bool IsTodaysFundJobCompleted(Subscription subscription, IClock clock, IEnumerable<AddingFundToCardRun> todaysRuns)
        {
            var today = clock.GetCurrentInstant().ToDateTimeUtc();
            return IsTodaysFundJobCompleted(subscription.MonthlyPaymentMoment, today, todaysRuns);
        }

        public static string GetAddingFundToCardJobNameForPaymentDay(SubscriptionMonthlyPaymentMoment moment, DateTime utcToday)
        {
            if (moment == SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth
                || (moment == SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth && utcToday.Day == 1))
            {
                return AddingFundToCardFirstDayOfTheMonthJobName;
            }

            if (moment == SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth
                || (moment == SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth && utcToday.Day == 15))
            {
                return AddingFundToCardFifteenthDayOfTheMonthJobName;
            }

            if (moment == SubscriptionMonthlyPaymentMoment.FirstDayOfTheWeek)
            {
                return AddingFundToCardFirstDayOfTheWeekJobName;
            }

            throw new ArgumentOutOfRangeException(nameof(moment), moment, "Not a payment day for this moment.");
        }

        public static bool IsMonthlyPaymentDay(SubscriptionMonthlyPaymentMoment moment, DateTime utcToday)
        {
            if (moment == SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth)
                return utcToday.Day == 1;
            if (moment == SubscriptionMonthlyPaymentMoment.FifteenthDayOfTheMonth)
                return utcToday.Day == 15;
            if (moment == SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth)
                return utcToday.Day == 1 || utcToday.Day == 15;
            return false;
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
