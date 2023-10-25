using GraphQL.DataLoader;
using NodaTime;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public interface IAddingFundTransactionGraphType : ITransactionGraphType
    {
        OffsetDateTime ExpirationDate();
        IDataLoaderResult<ProductGroupGraphType> ProductGroup(IAppUserContext ctx);
    }
}
