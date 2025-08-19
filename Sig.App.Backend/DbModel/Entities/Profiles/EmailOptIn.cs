using System.ComponentModel.DataAnnotations;

namespace Sig.App.Backend.DbModel.Entities.Profiles
{
    public class UserEmailOptIn : IHaveLongIdentifier
    {
        public long Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public AppUser User { get; set; }

        public bool CreatedCardPdfEmail { get; set; } = true;
        public bool MonthlyBalanceReportEmail { get; set; } = true;
        public bool MonthlyCardBalanceReportEmail { get; set; } = true;
        public bool SubscriptionExpirationEmail { get; set; } = true;
    }
}
