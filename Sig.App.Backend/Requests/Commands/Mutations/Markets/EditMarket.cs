using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Markets
{
    public class EditMarket : IRequestHandler<EditMarket.Input, EditMarket.Payload>
    {
        private readonly ILogger<EditMarket> logger;
        private readonly AppDbContext db;

        public EditMarket(ILogger<EditMarket> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            var marketId = request.MarketId.LongIdentifierForType<Market>();
            var market = await db.Markets.FirstOrDefaultAsync(x => x.Id == marketId, cancellationToken);

            if (market == null) throw new MarketNotFoundException();

            request.Name.IfSet(v => market.Name = v.Trim());
            request.RefundTransactionPassword.IfSet(v => market.SetRefundTransactionPassword(v.Trim()));

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"Market edited {market.Name} ({market.Id})");

            return new Payload
            {
                Market = new MarketGraphType(market)
            };
        }

        [MutationInput]
        public class Input : IRequest<Payload>, IHaveMarketId
        {
            public Id MarketId { get; set; }
            public Maybe<NonNull<string>> Name { get; set; }
            public Maybe<NonNull<string>> RefundTransactionPassword { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public MarketGraphType Market { get; set; }
        }

        public class MarketNotFoundException : RequestValidationException { }
    }
}
