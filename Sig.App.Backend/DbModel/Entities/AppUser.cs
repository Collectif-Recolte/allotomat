using Microsoft.AspNetCore.Identity;
using System;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Enums;

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

        public UserEmailOptIn EmailOptIn { get; set; }
    }
}
