using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.ProductGroups
{
    public class EditProductGroup : IRequestHandler<EditProductGroup.Input, EditProductGroup.Payload>
    {
        private readonly ILogger<EditProductGroup> logger;
        private readonly AppDbContext db;

        public EditProductGroup(ILogger<EditProductGroup> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] EditProductGroup({request.ProductGroupId}, {request.Name}, {request.Color}, {request.OrderOfAppearance})");
            var productGroupId = request.ProductGroupId.LongIdentifierForType<ProductGroup>();
            var productGroup = await db.ProductGroups.FirstOrDefaultAsync(x => x.Id == productGroupId, cancellationToken);

            if (productGroup == null)
            {
                logger.LogWarning("[Mutation] EditProductGroup - ProductGroupNotFoundException");
                throw new ProductGroupNotFoundException();
            }

            if (productGroup.Name == ProductGroupType.LOYALTY)
            {
                logger.LogWarning("[Mutation] EditProductGroup - CantEditLoyaltyProductGroup");
                throw new CantEditLoyaltyProductGroup();
            }

            request.Name.IfSet(x => productGroup.Name = x.Value.Trim());
            request.Color.IfSet(x => productGroup.Color = x);
            request.OrderOfAppearance.IfSet(x => productGroup.OrderOfAppearance = x);

            await db.SaveChangesAsync();

            logger.LogInformation($"[Mutation] EditProductGroup - Product group edited {productGroup.Id}");

            return new Payload()
            {
                ProductGroup = new ProductGroupGraphType(productGroup)
            };
        }

        [MutationInput]
        public class Input : IRequest<Payload>
        {
            public Id ProductGroupId { get; set; }
            public Maybe<NonNull<string>> Name { get; set; }
            public Maybe<ProductGroupColor> Color { get; set; }
            public Maybe<int> OrderOfAppearance { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public ProductGroupGraphType ProductGroup { get; set; }
        }

        public class ProductGroupNotFoundException : RequestValidationException { }
        public class CantEditLoyaltyProductGroup : RequestValidationException { }
    }
}
