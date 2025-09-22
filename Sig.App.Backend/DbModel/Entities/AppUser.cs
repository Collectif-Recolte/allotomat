using Microsoft.AspNetCore.Identity;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using System;

namespace Sig.App.Backend.DbModel.Entities
{
    public class AppUser : IdentityUser<string>, IHaveStringIdentifier
    {
        public AppUser() { }

        public AppUser(string userName)
        {
            UserName = userName;
        }

        public override string Email
        {
            get => UserName;
            set => base.Email = base.UserName = value;
        }

        public override string UserName
        {
            get => base.UserName;
            set => base.Email = base.UserName = value;
        }

        public UserType Type { get; set; }

        public UserProfile Profile { get; set; }

        public DateTime? LastAccessTimeUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public UserState State { get; set; } = UserState.Active;

        public UserStatus Status { get; set; } = UserStatus.Actived;

        public string EmailOptIn { get; set; } = string.Join(';',
                                                    new[] {Enums.EmailOptIn.CreatedCardPdfEmail,
                                                    Enums.EmailOptIn.MonthlyBalanceReportEmailJanuary,
                                                    Enums.EmailOptIn.MonthlyBalanceReportEmailFebruary,
                                                    Enums.EmailOptIn.MonthlyBalanceReportEmailMarch,
                                                    Enums.EmailOptIn.MonthlyBalanceReportEmailApril,
                                                    Enums.EmailOptIn.MonthlyBalanceReportEmailMay,
                                                    Enums.EmailOptIn.MonthlyBalanceReportEmailJune,
                                                    Enums.EmailOptIn.MonthlyBalanceReportEmailJuly,
                                                    Enums.EmailOptIn.MonthlyBalanceReportEmailAugust,
                                                    Enums.EmailOptIn.MonthlyBalanceReportEmailSeptember,
                                                    Enums.EmailOptIn.MonthlyBalanceReportEmailOctober,
                                                    Enums.EmailOptIn.MonthlyBalanceReportEmailNovember,
                                                    Enums.EmailOptIn.MonthlyBalanceReportEmailDecember,
                                                    Enums.EmailOptIn.MonthlyCardBalanceReportEmailJanuary,
                                                    Enums.EmailOptIn.MonthlyCardBalanceReportEmailFebruary,
                                                    Enums.EmailOptIn.MonthlyCardBalanceReportEmailMarch,
                                                    Enums.EmailOptIn.MonthlyCardBalanceReportEmailApril,
                                                    Enums.EmailOptIn.MonthlyCardBalanceReportEmailMay,
                                                    Enums.EmailOptIn.MonthlyCardBalanceReportEmailJune,
                                                    Enums.EmailOptIn.MonthlyCardBalanceReportEmailJuly,
                                                    Enums.EmailOptIn.MonthlyCardBalanceReportEmailAugust,
                                                    Enums.EmailOptIn.MonthlyCardBalanceReportEmailSeptember,
                                                    Enums.EmailOptIn.MonthlyCardBalanceReportEmailOctober,
                                                    Enums.EmailOptIn.MonthlyCardBalanceReportEmailNovember,
                                                    Enums.EmailOptIn.MonthlyCardBalanceReportEmailDecember,
                                                    Enums.EmailOptIn.SubscriptionExpirationEmail });
    }
}
