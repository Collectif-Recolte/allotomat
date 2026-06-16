using GraphQL.Conventions;
using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Services.Kiosk;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Transactions
{
    public class CreateKioskTransaction : IRequestHandler<CreateKioskTransaction.Input, CreateKioskTransaction.Payload>
    {
        private readonly ILogger<CreateKioskTransaction> logger;
        private readonly AppDbContext db;
        private readonly IMediator mediator;
        private readonly KioskJwtService kioskJwtService;

        public CreateKioskTransaction(ILogger<CreateKioskTransaction> logger, AppDbContext db, IMediator mediator, KioskJwtService kioskJwtService)
        {
            this.logger = logger;
            this.db = db;
            this.mediator = mediator;
            this.kioskJwtService = kioskJwtService;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation("[Mutation] CreateKioskTransaction");

            if (!string.IsNullOrEmpty(request.CardNumber))
            {
                logger.LogWarning("[Mutation] CreateKioskTransaction - ManualCardNumberNotAllowedException");
                throw new ManualCardNumberNotAllowedException();
            }

            if (!request.CardId.HasValue)
            {
                logger.LogWarning("[Mutation] CreateKioskTransaction - CardNotFoundException");
                throw new CardNotFoundException();
            }

            var resolved = await kioskJwtService.ResolveFromAuthToken(db, request.KioskToken, cancellationToken);

            var createResult = await mediator.Send(new CreateTransaction.Input
            {
                CardId = request.CardId,
                MarketId = resolved.Market.GetIdentifier(),
                CashRegisterId = resolved.CashRegister.GetIdentifier(),
                Transactions = request.Transactions.Select(x => new CreateTransaction.TransactionInput
                {
                    Amount = x.Amount,
                    ProductGroupId = x.ProductGroupId
                }).ToList()
            }, cancellationToken);

            logger.LogInformation("[Mutation] CreateKioskTransaction - Transaction created via kiosk for market {MarketName}", resolved.Market.Name);

            return new Payload
            {
                Transaction = createResult.Transaction
            };
        }

        [MutationInput]
        public class Input : IRequest<Payload>
        {
            public string KioskToken { get; set; }
            public Id? CardId { get; set; }
            public string CardNumber { get; set; }
            public List<TransactionInput> Transactions { get; set; }
        }

        [InputType]
        public class TransactionInput
        {
            public decimal Amount { get; set; }
            public Id ProductGroupId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public PaymentTransactionGraphType Transaction { get; set; }
        }

        public class CardNotFoundException : RequestValidationException { }
        public class ManualCardNumberNotAllowedException : RequestValidationException { }
    }
}
