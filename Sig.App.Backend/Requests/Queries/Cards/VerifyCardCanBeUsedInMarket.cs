using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.Cards
{
    public class VerifyCardCanBeUsedInMarket : IRequestHandler<VerifyCardCanBeUsedInMarket.Input, bool>
    {
        private readonly AppDbContext db;

        public VerifyCardCanBeUsedInMarket(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<bool> Handle(Input request, CancellationToken cancellationToken)
        {
            if (!request.CardId.IsIdentifierForType<Card>()) throw new CardNotFoundException();
            var card = await db.Cards.Where(x => x.Id == request.CardId.LongIdentifierForType<Card>()).FirstOrDefaultAsync();

            if (card == null) throw new CardNotFoundException();

            if (card.Status != DbModel.Enums.CardStatus.Assigned && card.Status != DbModel.Enums.CardStatus.GiftCard) throw new CardDeactivatedException();

            var projects = await db.ProjectMarkets.Where(x => x.MarketId == request.MarketId.LongIdentifierForType<Market>()).ToListAsync();

            if (projects.Find(x => x.ProjectId == card.ProjectId) != null)
            {
                return true;
            }

            throw new CardCantBeUsedInMarketException();
        }

        public class Input : HaveMarketIdAndCardId, IRequest<bool> { }

        public class CardNotFoundException : RequestValidationException { }
        public class CardDeactivatedException : RequestValidationException { }
        public class CardCantBeUsedInMarketException : RequestValidationException { }
    }
}
