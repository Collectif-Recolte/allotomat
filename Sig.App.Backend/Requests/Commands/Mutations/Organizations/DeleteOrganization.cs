using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Requests.Queries.Organizations;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Organizations
{
    public class DeleteOrganization : IRequestHandler<DeleteOrganization.Input>
    {
        private readonly ILogger<DeleteOrganization> logger;
        private readonly UserManager<AppUser> userManager;
        private readonly AppDbContext db;
        private readonly IMediator mediator;

        public DeleteOrganization(ILogger<DeleteOrganization> logger, AppDbContext db, UserManager<AppUser> userManager, IMediator mediator)
        {
            this.logger = logger;
            this.userManager = userManager;
            this.db = db;
            this.mediator = mediator;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] DeleteOrganization({request.OrganizationId})");
            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations
                .Include(x => x.Beneficiaries).ThenInclude(x => x.Subscriptions)
                .Include(x => x.Beneficiaries).ThenInclude(x => x.Card).ThenInclude(x => x.Transactions)
                .FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null)
            {
                logger.LogWarning("[Mutation] DeleteOrganization - OrganizationNotFoundException");
                throw new OrganizationNotFoundException();
            }

            var organizationManagers = await mediator.Send(new GetOrganizationManagers.Query
            {
                OrganizationId = organizationId
            });

            if (organizationManagers != null)
            {
                foreach (var manager in organizationManagers)
                {
                    await userManager.RemoveClaimAsync(manager, new Claim(AppClaimTypes.OrganizationManagerOf, organizationId.ToString()));
                    logger.LogInformation($"[Mutation] DeleteOrganization - Remove claim from manager {manager.Email}");
                }
            }

            db.SubscriptionBeneficiaries.RemoveRange(organization.Beneficiaries.SelectMany(x => x.Subscriptions));
            db.Transactions.RemoveRange(organization.Beneficiaries.SelectMany(x => x.Card.Transactions));

            db.Organizations.Remove(organization);

            await db.SaveChangesAsync();
            
            logger.LogInformation($"[Mutation] DeleteOrganization - Organization deleted ({organizationId}, {organization.Name})");
        }

        [MutationInput]
        public class Input : HaveOrganizationId, IRequest {}

        public class OrganizationNotFoundException : RequestValidationException { }
    }
}
