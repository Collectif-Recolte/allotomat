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
    public class AssignUnassignedCardToBeneficiary : IRequestHandler<AssignUnassignedCardToBeneficiary.Input, AssignUnassignedCardToBeneficiary.Payload>
    {
        private readonly ILogger<AssignUnassignedCardToBeneficiary> logger;
        private readonly AppDbContext db;

        public AssignUnassignedCardToBeneficiary(ILogger<AssignUnassignedCardToBeneficiary> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] AssignUnassignedCardToBeneficiary({request.BeneficiaryId})");
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

            var card = await db.Cards.FirstOrDefaultAsync(x => x.Status == CardStatus.Unassigned && x.ProjectId == beneficiary.Organization.ProjectId);
            if (card == null) throw new NoUnassignedCardAvailableException();

            card.Status = CardStatus.Assigned;
            beneficiary.Card = card;

            await db.SaveChangesAsync();

            return new Payload()
            {
                Beneficiary = beneficiary is OffPlatformBeneficiary ? new OffPlatformBeneficiaryGraphType(beneficiary as OffPlatformBeneficiary) : new BeneficiaryGraphType(beneficiary)
            };
        }

        [MutationInput]
        public class Input : HaveBeneficiaryId, IRequest<Payload> {}

        [MutationPayload]
        public class Payload
        {
            public IBeneficiaryGraphType Beneficiary { get; set; }
        }

        public class NoUnassignedCardAvailableException : RequestValidationException { }
        public class BeneficiaryNotFoundException : RequestValidationException { }
    }
}
