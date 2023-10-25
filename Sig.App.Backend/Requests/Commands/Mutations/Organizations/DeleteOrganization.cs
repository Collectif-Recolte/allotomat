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
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Requests.Queries.Markets;
using Sig.App.Backend.Requests.Queries.Organizations;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Organizations
{
    public class DeleteOrganization : AsyncRequestHandler<DeleteOrganization.Input>
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

        protected override async Task Handle(Input request, CancellationToken cancellationToken)
        {
            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations
                .Include(x => x.Beneficiaries).ThenInclude(x => x.Subscriptions)
                .Include(x => x.Beneficiaries).ThenInclude(x => x.Card).ThenInclude(x => x.Transactions)
                .FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null) throw new OrganizationNotFoundException();

            var organizationManagers = await mediator.Send(new GetOrganizationManagers.Query
            {
                OrganizationId = organizationId
            });

            if (organizationManagers != null)
            {
                foreach (var manager in organizationManagers)
                {
                    await userManager.RemoveClaimAsync(manager, new Claim(AppClaimTypes.OrganizationManagerOf, organizationId.ToString()));
                }
            }

            db.SubscriptionBeneficiaries.RemoveRange(organization.Beneficiaries.SelectMany(x => x.Subscriptions));
            db.Transactions.RemoveRange(organization.Beneficiaries.SelectMany(x => x.Card.Transactions));

            db.Organizations.Remove(organization);

            await db.SaveChangesAsync();
            
            logger.LogInformation($"Organization deleted ({organizationId}, {organization.Name})");
        }

        [MutationInput]
        public class Input : IRequest, IHaveOrganizationId
        {
            public Id OrganizationId { get; set; }
        }

        public class OrganizationNotFoundException : RequestValidationException { }
    }
}
