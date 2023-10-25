using GraphQL.Conventions;
using GraphQL.DataLoader;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public interface ITransactionGraphType
    {
        Id Id { get; }
        decimal Amount {get;}
        IDataLoaderResult<CardGraphType> Card(IAppUserContext ctx);
    }
}
