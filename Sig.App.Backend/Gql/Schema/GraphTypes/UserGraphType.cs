using Sig.App.Backend.Extensions;
using GraphQL.Conventions;
using System.Threading.Tasks;
using NodaTime;
using GraphQL.DataLoader;
using Sig.App.Backend.Authorization;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class UserGraphType
    {
        private readonly AppUser user;

        public Id Id => user.GetIdentifier();
        public NonNull<string> Email => user.Email;
        public UserType Type => user.Type;
        public bool IsConfirmed => user.EmailConfirmed;

        public UserGraphType(AppUser user)
        {
            this.user = user;
        }

        public IDataLoaderResult<IProfileGraphType> Profile(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProfileByUserId(user.Id);
        }

        [ApplyPolicy(AuthorizationPolicies.IsPCAAdmin)]
        public OffsetDateTime? LastConnectionTime()
        {
            if (user.LastAccessTimeUtc.HasValue)
            {
                return user.LastAccessTimeUtc.Value.FromUtcToOffsetDateTime();
            }

            return null;
        }

        public PermissionsGraphType Permissions(IAppUserContext ctx)
        {
            return new PermissionsGraphType(ctx.CurrentUser);
        }
    }
}