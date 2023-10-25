using GraphQL.Conventions;
using GraphQL.DataLoader;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Extensions;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class UserProfileGraphType : IProfileGraphType
    {
        public UserProfileGraphType(UserProfile profile)
        {
            this.profile = profile;
        }

        public IDataLoaderResult<UserGraphType> User(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadUser(profile.UserId);
        }

        private readonly UserProfile profile;

        public Id Id => profile.GetIdentifier();
        public string FirstName => profile.FirstName;
        public string LastName => profile.LastName;
    }
}

