using GraphQL.Conventions;
using GraphQL.DataLoader;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class FundGraphType
    {
        private readonly Fund fund;

        public Id Id => fund.GetIdentifier();
        public decimal Amount => fund.Amount;

        public FundGraphType(Fund fund)
        {
            this.fund = fund;
        }

        public IDataLoaderResult<ProductGroupGraphType> ProductGroup(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProductGroup(fund.ProductGroupId);
        }

        public IDataLoaderResult<CardGraphType> Card(IAppUserContext ctx)
        {
            if (fund.CardId.HasValue) return ctx.DataLoader.LoadCardById(fund.CardId.Value);
            return null;
        }
    }
}
