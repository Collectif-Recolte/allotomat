using GraphQL.Conventions;
using GraphQL.DataLoader;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    [Name("ProfileGraphType")]
    public interface IProfileGraphType
    {
        IDataLoaderResult<UserGraphType> User(IAppUserContext ctx);

        Id Id { get; }
        string FirstName { get; }
        string LastName { get; }

        public static IProfileGraphType Create(UserProfile profile) => new UserProfileGraphType(profile);
    }
}