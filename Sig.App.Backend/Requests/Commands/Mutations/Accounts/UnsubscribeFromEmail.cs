using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries
{
    public class UnsubscribeFromEmail : IRequestHandler<UnsubscribeFromEmail.Input>
    {
        private readonly ILogger<UnsubscribeFromEmail> logger;
        private readonly AppDbContext db;
        private readonly IAppUserContext ctx;

        public UnsubscribeFromEmail(ILogger<UnsubscribeFromEmail> logger, AppDbContext db, IAppUserContext ctx)
        {
            this.logger = logger;
            this.db = db;
            this.ctx = ctx;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] UnsubscribeFromEmail({request.EmailType})");
            var currentUserId = ctx.CurrentUser.GetUserId();

            var user = await db.Users.Include(x => x.EmailOptIn).FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken);

            if (user == null)
            {
                logger.LogWarning("[Mutation] UnsubscribeFromEmail - UserNotFoundException");
                throw new UserNotFoundException();
            }

            switch (request.EmailType)
            {
                case EmailOptIn.MonthlyBalanceReportEmail:
                    user.EmailOptIn.MonthlyBalanceReportEmail = false;
                    break;
                case EmailOptIn.MonthlyCardBalanceReportEmail:
                    user.EmailOptIn.MonthlyCardBalanceReportEmail = false;
                    break;
                case EmailOptIn.CreatedCardPdfEmail:
                    user.EmailOptIn.CreatedCardPdfEmail = false;
                    break;
                case EmailOptIn.SubscriptionExpirationEmail:
                    user.EmailOptIn.SubscriptionExpirationEmail = false;
                    break;
            }

            await db.SaveChangesAsync();
            logger.LogInformation($"[Mutation] UnsubscribeFromEmail - User unsubscribe from email ({user.Id}, {request.EmailType})");
        }

        [MutationInput]
        public class Input : IRequest
        {
            public EmailOptIn EmailType { get; set; }
        }

        public class UserNotFoundException : RequestValidationException { }
    }
}
