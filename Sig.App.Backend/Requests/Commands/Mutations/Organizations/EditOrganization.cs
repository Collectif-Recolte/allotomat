using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Organizations
{
    public class EditOrganization : IRequestHandler<EditOrganization.Input, EditOrganization.Payload>
    {
        private readonly ILogger<EditOrganization> logger;
        private readonly AppDbContext db;

        public EditOrganization(ILogger<EditOrganization> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] EditOrganization({request.OrganizationId}, {request.Name})");
            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations.FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null) throw new OrganizationNotFoundException();

            request.Name.IfSet(v => organization.Name = v.Trim());
            
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"Organization edited {organization.Name} ({organization.Id})");

            return new Payload
            {
                Organization = new OrganizationGraphType(organization)
            };
        }

        [MutationInput]
        public class Input : HaveOrganizationId, IRequest<Payload>
        {
            public Maybe<NonNull<string>> Name { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public OrganizationGraphType Organization { get; set; }
        }

        public class OrganizationNotFoundException : RequestValidationException { }
    }
}
