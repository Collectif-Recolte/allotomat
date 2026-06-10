using GraphQL.Conventions;
using MediatR;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Plugins.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.Cards
{
    public class VerifyCardCanBeUsedInKiosk : IRequestHandler<VerifyCardCanBeUsedInKiosk.Input, bool>
    {
        private readonly AppDbContext db;
        private readonly IMediator mediator;

        public VerifyCardCanBeUsedInKiosk(AppDbContext db, IMediator mediator)
        {
            this.db = db;
            this.mediator = mediator;
        }

        public async Task<bool> Handle(Input request, CancellationToken cancellationToken)
        {
            var resolved = await KioskCashRegisterResolver.Resolve(db, request.KioskToken, cancellationToken);

            if (!resolved.TokenFound || !resolved.IsOperational || resolved.MarketIsDisabled)
            {
                throw new KioskAccessInvalidException();
            }

            return await mediator.Send(new VerifyCardCanBeUsedInMarket.Input
            {
                CardId = request.CardId,
                MarketId = resolved.Market.GetIdentifier(),
                CashRegisterId = resolved.CashRegister.GetIdentifier()
            }, cancellationToken);
        }

        public class Input : HaveCardId, IRequest<bool>
        {
            public string KioskToken { get; set; }
        }

        public class KioskAccessInvalidException : RequestValidationException { }
    }
}
