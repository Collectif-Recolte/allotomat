using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Plugins.GraphQL;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Extensions;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Gql.Bases;

namespace Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries
{
    public class UnsubscribeBeneficiaryFromTransactionReceipt : IRequestHandler<UnsubscribeBeneficiaryFromTransactionReceipt.Input>
    {
        private readonly ILogger<UnsubscribeBeneficiaryFromTransactionReceipt> logger;
        private readonly AppDbContext db;

        public UnsubscribeBeneficiaryFromTransactionReceipt(ILogger<UnsubscribeBeneficiaryFromTransactionReceipt> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] UnsubscribeBeneficiaryFromTransactionReceipt({request.BeneficiaryId})");
            var beneficiaryId = request.BeneficiaryId.LongIdentifierForType<Beneficiary>();
            var beneficiary = await db.Beneficiaries
                .Include(x => x.Subscriptions).ThenInclude(x => x.Subscription)
                .FirstOrDefaultAsync(x => x.Id == beneficiaryId, cancellationToken);

            if (beneficiary == null)
            {
                logger.LogWarning("[Mutation] UnsubscribeBeneficiaryFromTransactionReceipt - BeneficiaryNotFoundException");
                throw new BeneficiaryNotFoundException();
            }

            if (string.IsNullOrEmpty(beneficiary.Email))
            {
                logger.LogWarning("[Mutation] UnsubscribeBeneficiaryFromTransactionReceipt - BeneficiaryDontHaveEmailException");
                throw new BeneficiaryDontHaveEmailException();
            }

            beneficiary.IsUnsubscribeToReceipt = true;

            await db.SaveChangesAsync();
            logger.LogInformation($"[Mutation] UnsubscribeBeneficiaryFromTransactionReceipt - Beneficiary unsubscribe from transaction receipt ({beneficiaryId}, {beneficiary.Firstname} {beneficiary.Lastname})");
        }

        [MutationInput]
        public class Input : HaveBeneficiaryId, IRequest {}

        public class BeneficiaryNotFoundException : RequestValidationException { }
        public class BeneficiaryDontHaveEmailException : RequestValidationException { }
    }
}
