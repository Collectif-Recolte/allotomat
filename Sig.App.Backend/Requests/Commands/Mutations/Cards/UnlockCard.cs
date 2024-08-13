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
    public class UnlockCard: IRequestHandler<UnlockCard.Input, UnlockCard.Payload>
    {
        private readonly ILogger<UnlockCard> logger;
        private readonly AppDbContext db;

        public UnlockCard(ILogger<UnlockCard> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] UnlockCard({request.CardId})");
            var cardId = request.CardId.LongIdentifierForType<Card>();
            var card = await db.Cards.FirstOrDefaultAsync(x => x.Id == cardId, cancellationToken);

            if (card == null)
            {
                throw new CardNotFoundException();
            }

            if (card.Status != CardStatus.Lost)
            {
                logger.LogWarning("[Mutation] UnlockCard - CardNotLostException");
                throw new CardNotLostException();
            }

            card.Status = CardStatus.Unassigned;

            await db.SaveChangesAsync();

            logger.LogInformation($"[Mutation] UnlockCard - Card ({card.Id}) is now Unassigned");

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
        public class CardNotLostException : RequestValidationException { }
    }
}
