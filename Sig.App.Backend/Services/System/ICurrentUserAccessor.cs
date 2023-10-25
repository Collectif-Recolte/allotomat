using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;

namespace Sig.App.Backend.Services.System
{
    public interface ICurrentUserAccessor
    {
        string GetCurrentUserId();
        ValueTask<AppUser> GetCurrentUser();
        bool IsUserType(UserType type);
    }
}
