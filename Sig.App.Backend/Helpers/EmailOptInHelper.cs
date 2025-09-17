using FluentEmail.Core;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Linq;

namespace Sig.App.Backend.Helpers
{
    public static class EmailOptInHelper
    {
        public static bool GetIfEmailOptIn(this AppUser user, EmailOptIn emailOptIn)
        {
            return user.EmailOptIn.Split(';').Any(x => x == emailOptIn.ToString());
        }

        public static void AddEmailOptIn(AppUser user, EmailOptIn emailOptIn)
        {
            if (user.EmailOptIn.IndexOf(emailOptIn.ToString()) == -1)
            {
                var emailsOptIns = user.EmailOptIn.Split(';');
                emailsOptIns.AddRange(new string[] { emailOptIn.ToString() });
                user.EmailOptIn = string.Join(';', emailsOptIns);
            }
        }

        public static void RemoveEmailOptIns(AppUser user, EmailOptIn[] emailOptIns)
        {
            foreach (var emailOpt in emailOptIns)
            {
                RemoveEmailOptIn(user, emailOpt);
            }
        }

        public static void RemoveEmailOptIn(AppUser user, EmailOptIn emailOptIn)
        {
            if (user.EmailOptIn.IndexOf(emailOptIn.ToString()) != -1)
            {
                user.EmailOptIn = string.Join(';', user.EmailOptIn.Split(';').Where(x => x != emailOptIn.ToString()));
            }
        }

        public static void SetEmailOptIn(AppUser user, EmailOptIn[] emailOptIns)
        {
            user.EmailOptIn = string.Join(';', emailOptIns.Select(x => x.ToString()));
        }

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
