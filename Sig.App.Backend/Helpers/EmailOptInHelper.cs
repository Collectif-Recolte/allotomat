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

        public static EmailOptIn? GetEmailOptInMonthlyBalanceReport(DateTime month)
        {
            switch (month.Month)
            {
                case 1:
                    return EmailOptIn.MonthlyBalanceReportEmailJanuary;
                case 2:
                    return EmailOptIn.MonthlyBalanceReportEmailFebruary;
                case 3:
                    return EmailOptIn.MonthlyBalanceReportEmailMarch;
                case 4:
                    return EmailOptIn.MonthlyBalanceReportEmailApril;
                case 5:
                    return EmailOptIn.MonthlyBalanceReportEmailMay;
                case 6:
                    return EmailOptIn.MonthlyBalanceReportEmailJune;
                case 7:
                    return EmailOptIn.MonthlyBalanceReportEmailJuly;
                case 8:
                    return EmailOptIn.MonthlyBalanceReportEmailAugust;
                case 9:
                    return EmailOptIn.MonthlyBalanceReportEmailSeptember;
                case 10:
                    return EmailOptIn.MonthlyBalanceReportEmailOctober;
                case 11:
                    return EmailOptIn.MonthlyBalanceReportEmailNovember;
                case 12:
                    return EmailOptIn.MonthlyBalanceReportEmailDecember;
                default:
                    return null;
            }
        }

        public static EmailOptIn? GetEmailOptInMonthlyCardBalanceReport(DateTime month)
        {
            switch (month.Month)
            {
                case 1:
                    return EmailOptIn.MonthlyCardBalanceReportEmailJanuary;
                case 2:
                    return EmailOptIn.MonthlyCardBalanceReportEmailFebruary;
                case 3:
                    return EmailOptIn.MonthlyCardBalanceReportEmailMarch;
                case 4:
                    return EmailOptIn.MonthlyCardBalanceReportEmailApril;
                case 5:
                    return EmailOptIn.MonthlyCardBalanceReportEmailMay;
                case 6:
                    return EmailOptIn.MonthlyCardBalanceReportEmailJune;
                case 7:
                    return EmailOptIn.MonthlyCardBalanceReportEmailJuly;
                case 8:
                    return EmailOptIn.MonthlyCardBalanceReportEmailAugust;
                case 9:
                    return EmailOptIn.MonthlyCardBalanceReportEmailSeptember;
                case 10:
                    return EmailOptIn.MonthlyCardBalanceReportEmailOctober;
                case 11:
                    return EmailOptIn.MonthlyCardBalanceReportEmailNovember;
                case 12:
                    return EmailOptIn.MonthlyCardBalanceReportEmailDecember;
                default:
                    return null;
            }
        }
    }
}
