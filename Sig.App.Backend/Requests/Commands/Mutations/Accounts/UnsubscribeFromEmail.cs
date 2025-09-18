using System.Linq;
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
using Sig.App.Backend.Helpers;

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

            var user = await db.Users.FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken);

            if (user == null)
            {
                logger.LogWarning("[Mutation] UnsubscribeFromEmail - UserNotFoundException");
                throw new UserNotFoundException();
            }

            if (EmailOptInHelper.MonthlyBalanceReportEmailOptIns.Contains(request.EmailType))
                user.RemoveEmailOptIns(EmailOptInHelper.MonthlyBalanceReportEmailOptIns);
            else if (EmailOptInHelper.MonthlyCardBalanceReportEmailOptIns.Contains(request.EmailType))
                user.RemoveEmailOptIns(EmailOptInHelper.MonthlyCardBalanceReportEmailOptIns);
            else
                user.RemoveEmailOptIns(request.EmailType);

            await db.SaveChangesAsync(cancellationToken);

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
