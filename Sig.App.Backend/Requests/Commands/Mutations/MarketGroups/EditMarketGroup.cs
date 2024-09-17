using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.MarketGroups
{
    public class EditMarketGroup : IRequestHandler<EditMarketGroup.Input, EditMarketGroup.Payload>
    {
        private readonly ILogger<EditMarketGroup> logger;
        private readonly AppDbContext db;

        public EditMarketGroup(ILogger<EditMarketGroup> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] EditMarketGroup({request.Name})");
            var marketGroupId = request.MarketGroupId.LongIdentifierForType<MarketGroup>();
            var marketGroup = await db.MarketGroups.FirstOrDefaultAsync(x => x.Id == marketGroupId, cancellationToken);

            if (marketGroup == null)
            {
                logger.LogWarning("[Mutation] EditMarketGroup - MarketGroupNotFoundException");
                throw new MarketGroupNotFoundException();
            }

            request.Name.IfSet(v => marketGroup.Name = v.Trim());

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] EditMarketGroup - Market group edited {marketGroup.Name} ({marketGroup.Id})");

            return new Payload
            {
                MarketGroup = new MarketGroupGraphType(marketGroup)
            };
        }

        [MutationInput]
        public class Input : HaveMarketGroupId, IRequest<Payload>
        {
            public Maybe<NonNull<string>> Name { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public MarketGroupGraphType MarketGroup { get; set; }
        }

        public class MarketGroupNotFoundException : RequestValidationException { }
    }
}
