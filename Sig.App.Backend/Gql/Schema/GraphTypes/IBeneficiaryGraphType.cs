using GraphQL.Conventions;
using GraphQL.DataLoader;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public interface IBeneficiaryGraphType
    {
        Id Id { get; }
        NonNull<string> Firstname { get; }
        NonNull<string> Lastname { get; }
        string Email { get; }
        string Phone { get; }
        string Address { get; }
        string Notes { get; }
        string Id1 { get; }
        string Id2 { get; }
        string PostalCode { get; }

        IDataLoaderResult<OrganizationGraphType> Organization(IAppUserContext ctx);
        IDataLoaderResult<CardGraphType> Card(IAppUserContext ctx);
    }
}
