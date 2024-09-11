using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Organizations
{
    public class AddMarketToOrganization : IRequestHandler<AddMarketToOrganization.Input, AddMarketToOrganization.Payload>
    {
        private readonly ILogger<AddMarketToOrganization> logger;
        private readonly AppDbContext db;

        public AddMarketToOrganization(ILogger<AddMarketToOrganization> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AddMarketToOrganization({request.MarketId}, {request.OrganizationId})");
            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations.Include(x => x.Markets).FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null)
            {
                logger.LogWarning("[Mutation] AddMarketToOrganization - OrganizationNotFoundException");
                throw new OrganizationNotFoundException();
            }

            var marketId = request.MarketId.LongIdentifierForType<Market>();
            var market = await db.Markets.FirstOrDefaultAsync(x => x.Id == marketId, cancellationToken);

            if (market == null)
            {
                logger.LogWarning("[Mutation] AddMarketToOrganization - MarketNotFoundException");
                throw new MarketNotFoundException();
            }

            if (organization.Markets.Any(x => x.MarketId == marketId))
            {
                logger.LogWarning("[Mutation] AddMarketToOrganization - MarketAlreadyInOrganizationException");
                throw new MarketAlreadyInOrganizationException();
            }

            organization.Markets.Add(new OrganizationMarket()
            {
                MarketId = marketId,
                OrganizationId = organizationId
            });

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] AddMarketToOrganization - Market {market.Name} added to organization {organization.Name}");

            return new Payload()
            {
                Organization = new OrganizationGraphType(organization)
            };
        }

        public class OrganizationNotFoundException : RequestValidationException { }
        public class MarketNotFoundException : RequestValidationException { }
        public class MarketAlreadyInOrganizationException : RequestValidationException { }

        [MutationInput]
        public class Input : HaveOrganizationIdAndMarketId, IRequest<Payload> { }

        [MutationPayload]
        public class Payload
        {
            public OrganizationGraphType Organization { get; set; }
        }
    }
}
