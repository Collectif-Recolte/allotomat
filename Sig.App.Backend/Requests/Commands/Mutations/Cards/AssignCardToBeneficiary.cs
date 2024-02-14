using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
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
    public class AssignCardToBeneficiary : IRequestHandler<AssignCardToBeneficiary.Input, AssignCardToBeneficiary.Payload>
    {
        private readonly ILogger<AssignCardToBeneficiary> logger;
        private readonly AppDbContext db;

        public AssignCardToBeneficiary(ILogger<AssignCardToBeneficiary> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AssignCardToBeneficiary({request.BeneficiaryId}, {request.CardId})");
            long beneficiaryId;
            if (request.BeneficiaryId.IsIdentifierForType(typeof(Beneficiary)))
            {
                beneficiaryId = request.BeneficiaryId.LongIdentifierForType<Beneficiary>();
            }
            else
            {
                beneficiaryId = request.BeneficiaryId.LongIdentifierForType<OffPlatformBeneficiary>();
            }
            var beneficiary = await db.Beneficiaries.Include(x => x.Organization).FirstOrDefaultAsync(x => x.Id == beneficiaryId, cancellationToken);

            if (beneficiary == null) throw new BeneficiaryNotFoundException();

            var card = await db.Cards.FirstOrDefaultAsync(x => x.ProgramCardId == request.CardId && x.ProjectId == beneficiary.Organization.ProjectId, cancellationToken);

            if (card == null) throw new CardNotFoundException();

            if (card.Status == CardStatus.Assigned) throw new CardAlreadyAssignException();
            if (card.Status == CardStatus.Lost) throw new CardLostException();
            if (card.Status == CardStatus.Deactivated) throw new CardDeactivatedException();
            if (card.Status == CardStatus.GiftCard) throw new CardAlreadyGiftCardException();

            if(card.ProjectId != beneficiary.Organization.ProjectId) throw new CardNotInProjectException();

            card.Status = CardStatus.Assigned;

            beneficiary.Card = card;

            await db.SaveChangesAsync();

            logger.LogInformation($"Card ({card.Id}) assign  to {beneficiary.Firstname} {beneficiary.Lastname} ({beneficiary.Id})");

            return new Payload() {
                Beneficiary = beneficiary is OffPlatformBeneficiary ? new OffPlatformBeneficiaryGraphType(beneficiary as OffPlatformBeneficiary) : new BeneficiaryGraphType(beneficiary)
            };
        }

        [MutationInput]
        public class Input : HaveBeneficiaryId, IRequest<Payload>
        {
            public long CardId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public IBeneficiaryGraphType Beneficiary { get; set; }
        }

        public class CardNotFoundException : RequestValidationException { }
        public class BeneficiaryNotFoundException : RequestValidationException { }
        public class CardAlreadyAssignException : RequestValidationException { }
        public class CardLostException : RequestValidationException { }
        public class CardDeactivatedException : RequestValidationException { }
        public class CardAlreadyGiftCardException : RequestValidationException { }
        public class CardNotInProjectException : RequestValidationException { }
    }
}
