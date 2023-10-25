using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Sig.App.Backend.Requests.Commands.Mutations.ProductGroups.EditProductGroup;

namespace Sig.App.Backend.Requests.Commands.Mutations.ProductGroups
{
    public class DeleteProductGroup : AsyncRequestHandler<DeleteProductGroup.Input>
    {
        private readonly ILogger<DeleteProductGroup> logger;
        private readonly AppDbContext db;

        public DeleteProductGroup(ILogger<DeleteProductGroup> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        protected override async Task Handle(Input request, CancellationToken cancellationToken)
        {
            var productGroupId = request.ProductGroupId.LongIdentifierForType<ProductGroup>();
            var productGroup = await db.ProductGroups.Include(x => x.Types).FirstOrDefaultAsync(x => x.Id == productGroupId, cancellationToken);

            if (productGroup == null) throw new ProductGroupNotFoundException();

            if (productGroup.Name == ProductGroupType.LOYALTY) throw new CantDeleteLoyaltyProductGroup();

            if (HaveSubscriptions(productGroup)) throw new ProductGroupCantHaveSubscriptionsException();

            db.ProductGroups.Remove(productGroup);

            await db.SaveChangesAsync();
            logger.LogInformation($"Product group deleted ({productGroupId})");
        }

        private bool HaveSubscriptions(ProductGroup productGroup)
        {
            return productGroup.Types.Any();
        }

        [MutationInput]
        public class Input : IRequest
        {
            public Id ProductGroupId { get; set; }
        }

        public class ProductGroupNotFoundException : RequestValidationException { }
        public class ProductGroupCantHaveSubscriptionsException : RequestValidationException { }
        public class CantDeleteLoyaltyProductGroup : RequestValidationException { }
    }
}
