using GraphQL.Conventions;
using GraphQL.DataLoader;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using System.Collections.Generic;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class ProductGroupGraphType
    {
        private readonly ProductGroup productGroup;

        public ProductGroupGraphType(ProductGroup productGroup)
        {
            this.productGroup = productGroup;
        }

        public Id Id => productGroup.GetIdentifier();

        public IDataLoaderResult<ProjectGraphType> Project(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProject(productGroup.ProjectId);
        }

        public IDataLoaderResult<IEnumerable<SubscriptionTypeGraphType>> Types(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProductGroupSubscriptionType(Id.LongIdentifierForType<ProductGroup>());
        }

        public string Name => productGroup.Name;
        public ProductGroupColor Color => productGroup.Color;
        public int OrderOfAppearance => productGroup.OrderOfAppearance;
    }
}
