using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
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

            if (request.CashRegisterId.HasValue)
            {
                var cashRegisterId = request.CashRegisterId.Value.LongIdentifierForType<CashRegister>();
                var cashRegister = await db.CashRegisters.Include(x => x.MarketGroups).ThenInclude(x => x.MarketGroup).Where(x => x.Id == cashRegisterId).FirstOrDefaultAsync();

                if (!cashRegister.MarketGroups.Select(x => x.MarketGroup).Any(x => x.ProjectId == card.ProjectId))
                {
                    throw new CardCantBeUsedWithCashRegisterException();
                }
            }

            var projects = await db.ProjectMarkets.Where(x => x.MarketId == request.MarketId.LongIdentifierForType<Market>()).ToListAsync();
            var projectMarket = projects.Find(x => x.ProjectId == card.ProjectId);

            if (projectMarket != null)
            {
                return true;
            }

            throw new CardCantBeUsedInMarketException();
        }

        public class Input : HaveMarketIdAndCardId, IRequest<bool>
        {
            public Id? CashRegisterId { get; set; }
        }

        public class CardNotFoundException : RequestValidationException { }
        public class CardDeactivatedException : RequestValidationException { }
        public class CardCantBeUsedInMarketException : RequestValidationException { }
        public class CardCantBeUsedWithCashRegisterException : RequestValidationException { }
    }
}
