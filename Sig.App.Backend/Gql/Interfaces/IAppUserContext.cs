using GraphQL.Conventions;
using System.Security.Claims;

namespace Sig.App.Backend.Gql.Interfaces
{
    public interface IAppUserContext : IUserContext
    {
        ClaimsPrincipal CurrentUser { get; }
        string CurrentUserId { get; }
        DataLoader DataLoader { get; }
    }
}