using MediatR;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Services.Kiosk;
using Sig.App.Backend.Plugins.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.Cards
{
    public class GetKioskCard : IRequestHandler<GetKioskCard.Input, CardGraphType>
    {
        private readonly AppDbContext db;
        private readonly IMediator mediator;
        private readonly KioskJwtService kioskJwtService;

        public GetKioskCard(AppDbContext db, IMediator mediator, KioskJwtService kioskJwtService)
        {
            this.db = db;
            this.mediator = mediator;
            this.kioskJwtService = kioskJwtService;
        }

        public async Task<CardGraphType> Handle(Input request, CancellationToken cancellationToken)
        {
            var resolved = await kioskJwtService.ResolveFromAuthToken(db, request.KioskToken, cancellationToken);

            var canBeUsed = await mediator.Send(new VerifyCardCanBeUsedInMarket.Input
            {
                CardId = request.CardId,
                MarketId = resolved.Market.GetIdentifier(),
                CashRegisterId = resolved.CashRegister.GetIdentifier()
            }, cancellationToken);

            if (!canBeUsed)
            {
                throw new CardNotFoundException();
            }

            var cardId = request.CardId.LongIdentifierForType<Card>();
            var card = await db.Cards.FirstOrDefaultAsync(x => x.Id == cardId, cancellationToken);

            if (card == null)
            {
                throw new CardNotFoundException();
            }

            return new CardGraphType(card);
        }

        public class Input : HaveCardId, IRequest<CardGraphType>
        {
            public string KioskToken { get; set; }
        }

        public class CardNotFoundException : RequestValidationException { }
    }
}
