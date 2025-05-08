using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Subscriptions
{
    public class UnarchiveSubscription : IRequestHandler<UnarchiveSubscription.Input, UnarchiveSubscription.Payload>
    {
        private readonly ILogger<UnarchiveSubscription> logger;
        private readonly AppDbContext db;

        public UnarchiveSubscription(ILogger<UnarchiveSubscription> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] UnarchiveSubscription({request.SubscriptionId})");
            var subscriptionId = request.SubscriptionId.LongIdentifierForType<Subscription>();
            var subscription = await db.Subscriptions.Include(x => x.Types).FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

            if (subscription == null)
            {
                logger.LogWarning("[Mutation] UnarchiveSubscription - SubscriptionNotFoundException");
                throw new SubscriptionNotFoundException();
            }

            subscription.IsArchived = false;
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] UnarchiveSubscription - Subscription {subscription.Name} ({subscription.Id}) unarchived");

            return new Payload()
            {
                Subscription = new SubscriptionGraphType(subscription)
            };
        }

        [MutationInput]
        public class Input : HaveSubscriptionId, IRequest<Payload> {}

        [MutationPayload]
        public class Payload
        {
            public SubscriptionGraphType Subscription { get; set; }
        }

        public class SubscriptionNotFoundException : RequestValidationException { }
    }
}
