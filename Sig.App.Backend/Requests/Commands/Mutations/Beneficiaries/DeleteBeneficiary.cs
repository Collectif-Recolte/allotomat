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
using NodaTime;
using System.Linq;
using Sig.App.Backend.Gql.Bases;

namespace Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries
{
    public class DeleteBeneficiary : IRequestHandler<DeleteBeneficiary.Input>
    {
        private readonly ILogger<DeleteBeneficiary> logger;
        private readonly AppDbContext db;
        private readonly IClock clock;

        public DeleteBeneficiary(ILogger<DeleteBeneficiary> logger, AppDbContext db, IClock clock)
        {
            this.logger = logger;
            this.db = db;
            this.clock = clock;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] DeleteBeneficiary({request.BeneficiaryId})");
            var beneficiaryId = request.BeneficiaryId.LongIdentifierForType<Beneficiary>();
            var beneficiary = await db.Beneficiaries
                .Include(x => x.Subscriptions).ThenInclude(x => x.Subscription)
                .FirstOrDefaultAsync(x => x.Id == beneficiaryId, cancellationToken);

            if (beneficiary == null)
            {
                logger.LogWarning("[Mutation] DeleteBeneficiary - BeneficiaryNotFoundException");
                throw new BeneficiaryNotFoundException();
            }
            
            if (HaveAnyActiveSubscription(beneficiary))
            {
                logger.LogWarning("[Mutation] DeleteBeneficiary - BeneficiaryCantHaveActiveSubscriptionException");
                throw new BeneficiaryCantHaveActiveSubscriptionException();
            }

            if (beneficiary.CardId != null)
            {
                logger.LogWarning("[Mutation] DeleteBeneficiary - BeneficiaryCantHaveCardException");
                throw new BeneficiaryCantHaveCardException();
            }

            db.SubscriptionBeneficiaries.RemoveRange(beneficiary.Subscriptions);
            db.Beneficiaries.Remove(beneficiary);

            var transactions = await db.Transactions.Where(x => x.BeneficiaryId == beneficiaryId).ToListAsync();

            foreach (var transaction in transactions)
            {
                transaction.Beneficiary = null;
                transaction.BeneficiaryId = null;
            }

            await db.SaveChangesAsync();
            logger.LogInformation($"Beneficiary deleted ({beneficiaryId}, {beneficiary.Firstname} {beneficiary.Lastname})");
        }

        private bool HaveAnyActiveSubscription(Beneficiary beneficiary)
        {
            var haveAnyActiveSubscription = false;
            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            foreach (var sub in beneficiary.Subscriptions)
            {
                if (sub.Subscription.StartDate <= today && sub.Subscription.EndDate >= today)
                {
                    haveAnyActiveSubscription = true;
                }
            }

            return haveAnyActiveSubscription;
        }

        [MutationInput]
        public class Input : HaveBeneficiaryId, IRequest {}

        public class BeneficiaryNotFoundException : RequestValidationException { }
        public class BeneficiaryCantHaveActiveSubscriptionException : RequestValidationException { }
        public class BeneficiaryCantHaveCardException : RequestValidationException { }
    }
}
