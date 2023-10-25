using System;
using System.ComponentModel.DataAnnotations;

namespace Sig.App.Backend.DbModel.Entities.Profiles
{
    public class UserProfile : IHaveLongIdentifier
    {
        public UserProfile() { }

        public UserProfile(UserProfile copyFrom)
        {
            if (copyFrom == null) return;
            FirstName = copyFrom.FirstName;
            LastName = copyFrom.LastName;
        }

        public long Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public AppUser User { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }

        public DateTime UpdateTimeUtc { get; set; }
    }
}