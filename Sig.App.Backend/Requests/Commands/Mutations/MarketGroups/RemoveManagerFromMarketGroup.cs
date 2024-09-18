using GraphQL.Conventions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.MarketGroups
{
    public class RemoveManagerFromMarketGroup : IRequestHandler<RemoveManagerFromMarketGroup.Input, RemoveManagerFromMarketGroup.Payload>
    {
        private readonly ILogger<RemoveManagerFromMarketGroup> logger;
        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;

        public RemoveManagerFromMarketGroup(ILogger<RemoveManagerFromMarketGroup> logger, AppDbContext db, UserManager<AppUser> userManager)
        {
            this.logger = logger;
            this.db = db;
            this.userManager = userManager;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] RemoveManagerFromMarketGroup({request.MarketGroupId}, {request.ManagerId})");
            var marketGroupId = request.MarketGroupId.LongIdentifierForType<MarketGroup>();
            var marketGroup = await db.MarketGroups.FirstOrDefaultAsync(x => x.Id == marketGroupId, cancellationToken);

            if (marketGroup == null)
            {
                logger.LogWarning("[Mutation] RemoveManagerFromMarketGroup - MarketGroupNotFoundException");
                throw new MarketGroupNotFoundException();
            }
            
            var manager = await db.Users.FirstOrDefaultAsync(x => x.Id == request.ManagerId.IdentifierForType<AppUser>());

            if (manager == null)
            {
                logger.LogWarning("[Mutation] RemoveManagerFromMarketGroup - ManagerNotFoundException");
                throw new ManagerNotFoundException();
            }

            await userManager.RemoveClaimAsync(manager, new Claim(AppClaimTypes.MarketGroupManagerOf, marketGroup.Id.ToString()));

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] RemoveManagerFromMarketGroup - MarketGroup manager {manager.Email} remove from MarketGroup {marketGroup.Name} ({marketGroup.Id})");

            return new Payload
            {
                MarketGroup = new MarketGroupGraphType(marketGroup)
            };
        }

        public class MarketGroupNotFoundException : RequestValidationException { }
        public class ManagerNotFoundException : RequestValidationException { }


        [MutationInput]
        public class Input : HaveMarketGroupId, IRequest<Payload>
        {
            public Id ManagerId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public MarketGroupGraphType MarketGroup { get; set; }
        }
    }
}