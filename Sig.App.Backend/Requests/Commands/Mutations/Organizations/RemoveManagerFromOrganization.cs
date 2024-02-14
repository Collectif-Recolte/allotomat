using GraphQL.Conventions;
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
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Organizations
{
    public class RemoveManagerFromOrganization : IRequestHandler<RemoveManagerFromOrganization.Input, RemoveManagerFromOrganization.Payload>
    {
        private readonly ILogger<RemoveManagerFromOrganization> logger;
        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;

        public RemoveManagerFromOrganization(ILogger<RemoveManagerFromOrganization> logger, AppDbContext db, UserManager<AppUser> userManager)
        {
            this.logger = logger;
            this.db = db;
            this.userManager = userManager;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] RemoveManagerFromOrganization({request.OrganizationId}, {request.ManagerId})");
            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations.FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null) throw new OrganizationNotFoundException();
            
            var manager = await db.Users.FirstOrDefaultAsync(x => x.Id == request.ManagerId.IdentifierForType<AppUser>());

            if (manager == null) throw new ManagerNotFoundException();

            await userManager.RemoveClaimAsync(manager, new Claim(AppClaimTypes.OrganizationManagerOf, organization.Id.ToString()));

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"Organization manager {manager.Email} remove from organization {organization.Name} ({organization.Id})");

            return new Payload
            {
                Organization = new OrganizationGraphType(organization)
            };
        }

        public class OrganizationNotFoundException : RequestValidationException { }
        public class ManagerNotFoundException : RequestValidationException { }


        [MutationInput]
        public class Input : HaveOrganizationId, IRequest<Payload>
        {
            public Id ManagerId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public OrganizationGraphType Organization { get; set; }
        }
    }
}