using FluentEmail.Core;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sig.App.Backend.Helpers
{
    public static class EmailOptInHelper
    {
        public static EmailOptIn[] GetEmailOptIns(this AppUser user)
        {
            return user.EmailOptIn.Split(';')
                .Where(x => Enum.TryParse<EmailOptIn>(x, out _))
                .Select(Enum.Parse<EmailOptIn>)
                .ToArray();
        }

        public static void SetEmailOptIns(this AppUser user, IEnumerable<EmailOptIn> emailOptIns)
        {
            user.EmailOptIn = string.Join(';', emailOptIns.Distinct());
        }

        public static void AddEmailOptIns(this AppUser user, params EmailOptIn[] emailOptIn)
        {
            SetEmailOptIns(user, GetEmailOptIns(user).Concat(emailOptIn));
        }

        public static void RemoveEmailOptIns(this AppUser user, params EmailOptIn[] emailOptIns)
        {
            SetEmailOptIns(user, GetEmailOptIns(user).Except(emailOptIns));
        }

        public static bool IsEmailOptedIn(this AppUser user, EmailOptIn emailOptIn)
        {
            return GetEmailOptIns(user).Contains(emailOptIn);
        }

        public static readonly EmailOptIn[] MonthlyBalanceReportEmailOptIns =
        [
            EmailOptIn.MonthlyBalanceReportEmailJanuary,
            EmailOptIn.MonthlyBalanceReportEmailFebruary,
            EmailOptIn.MonthlyBalanceReportEmailMarch,
            EmailOptIn.MonthlyBalanceReportEmailApril,
            EmailOptIn.MonthlyBalanceReportEmailMay,
            EmailOptIn.MonthlyBalanceReportEmailJune,
            EmailOptIn.MonthlyBalanceReportEmailJuly,
            EmailOptIn.MonthlyBalanceReportEmailAugust,
            EmailOptIn.MonthlyBalanceReportEmailSeptember,
            EmailOptIn.MonthlyBalanceReportEmailOctober,
            EmailOptIn.MonthlyBalanceReportEmailNovember,
            EmailOptIn.MonthlyBalanceReportEmailDecember
        ];

        public static readonly EmailOptIn[] MonthlyCardBalanceReportEmailOptIns =
        [
            EmailOptIn.MonthlyBalanceReportEmailJanuary,
            EmailOptIn.MonthlyBalanceReportEmailFebruary,
            EmailOptIn.MonthlyBalanceReportEmailMarch,
            EmailOptIn.MonthlyBalanceReportEmailApril,
            EmailOptIn.MonthlyBalanceReportEmailMay,
            EmailOptIn.MonthlyBalanceReportEmailJune,
            EmailOptIn.MonthlyBalanceReportEmailJuly,
            EmailOptIn.MonthlyBalanceReportEmailAugust,
            EmailOptIn.MonthlyBalanceReportEmailSeptember,
            EmailOptIn.MonthlyBalanceReportEmailOctober,
            EmailOptIn.MonthlyBalanceReportEmailNovember,
            EmailOptIn.MonthlyBalanceReportEmailDecember
        ];

        public static EmailOptIn GetEmailOptInMonthlyBalanceReport(DateTime month)
        {
            return month.Month switch
            {
                1 => EmailOptIn.MonthlyBalanceReportEmailJanuary,
                2 => EmailOptIn.MonthlyBalanceReportEmailFebruary,
                3 => EmailOptIn.MonthlyBalanceReportEmailMarch,
                4 => EmailOptIn.MonthlyBalanceReportEmailApril,
                5 => EmailOptIn.MonthlyBalanceReportEmailMay,
                6 => EmailOptIn.MonthlyBalanceReportEmailJune,
                7 => EmailOptIn.MonthlyBalanceReportEmailJuly,
                8 => EmailOptIn.MonthlyBalanceReportEmailAugust,
                9 => EmailOptIn.MonthlyBalanceReportEmailSeptember,
                10 => EmailOptIn.MonthlyBalanceReportEmailOctober,
                11 => EmailOptIn.MonthlyBalanceReportEmailNovember,
                12 => EmailOptIn.MonthlyBalanceReportEmailDecember,
                _ => throw new InvalidOperationException("Bad month value")
            };
        }

        public static EmailOptIn GetEmailOptInMonthlyCardBalanceReport(DateTime month)
        {
            return month.Month switch
            {
                1 => EmailOptIn.MonthlyCardBalanceReportEmailJanuary,
                2 => EmailOptIn.MonthlyCardBalanceReportEmailFebruary,
                3 => EmailOptIn.MonthlyCardBalanceReportEmailMarch,
                4 => EmailOptIn.MonthlyCardBalanceReportEmailApril,
                5 => EmailOptIn.MonthlyCardBalanceReportEmailMay,
                6 => EmailOptIn.MonthlyCardBalanceReportEmailJune,
                7 => EmailOptIn.MonthlyCardBalanceReportEmailJuly,
                8 => EmailOptIn.MonthlyCardBalanceReportEmailAugust,
                9 => EmailOptIn.MonthlyCardBalanceReportEmailSeptember,
                10 => EmailOptIn.MonthlyCardBalanceReportEmailOctober,
                11 => EmailOptIn.MonthlyCardBalanceReportEmailNovember,
                12 => EmailOptIn.MonthlyCardBalanceReportEmailDecember,
                _ => throw new InvalidOperationException("Bad month value")
            };
        }
    }
}
