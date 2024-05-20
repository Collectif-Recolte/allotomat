using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Cards
{
    public class EnableCard : IRequestHandler<EnableCard.Input, EnableCard.Payload>
    {
        private readonly ILogger<EnableCard> logger;
        private readonly AppDbContext db;

        public EnableCard(ILogger<EnableCard> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] EnableCard({request.CardId})");
            var cardId = request.CardId.LongIdentifierForType<Card>();
            var card = await db.Cards.FirstOrDefaultAsync(x => x.Id == cardId, cancellationToken);

            if (card == null)
            {
                throw new CardNotFoundException();
            }

            if (card.Status == CardStatus.Unassigned || card.Status == CardStatus.Lost || card.Status == CardStatus.Deactivated)
            {
                logger.LogWarning("[Mutation] EnableCard - CardNotAssignException");
                throw new CardNotAssignException();
            }

            card.IsDisabled = false;

            await db.SaveChangesAsync();

            logger.LogInformation($"[Mutation] EnableCard - Card ({card.Id}) is now enable");

            return new Payload() {
                Card = new CardGraphType(card)
            };
        }

        [MutationInput]
        public class Input : HaveCardId, IRequest<Payload> {}

        [MutationPayload]
        public class Payload
        {
            public CardGraphType Card { get; set; }
        }

        public class CardNotFoundException : RequestValidationException { }
        public class CardNotAssignException : RequestValidationException { }
    }
}
