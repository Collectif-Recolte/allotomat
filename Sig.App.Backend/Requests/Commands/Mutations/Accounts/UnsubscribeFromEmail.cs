using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Collections.Generic;
using System.Linq;
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

            var user = await db.Users.FirstOrDefaultAsync(x => x.Id == currentUserId, cancellationToken);

            if (user == null)
            {
                logger.LogWarning("[Mutation] UnsubscribeFromEmail - UserNotFoundException");
                throw new UserNotFoundException();
            }

            if (user.EmailOptIn.Split(';').Any(x => x == request.EmailType.ToString()))
            {
                var emailOptIn = user.EmailOptIn.Split(';');
                IEnumerable<string> emailOptInFilter = null;

                switch (request.EmailType)
                {
                    case EmailOptIn.CreatedCardPdfEmail:
                    {
                        emailOptInFilter = emailOptIn.Where(x => x != EmailOptIn.CreatedCardPdfEmail.ToString());
                        break;
                    }
                    case EmailOptIn.SubscriptionExpirationEmail:
                    {
                        emailOptInFilter = emailOptIn.Where(x => x != EmailOptIn.SubscriptionExpirationEmail.ToString());
                        break;
                    }
                    case EmailOptIn.MonthlyBalanceReportEmailJanuary:
                    case EmailOptIn.MonthlyBalanceReportEmailFebruary:
                    case EmailOptIn.MonthlyBalanceReportEmailMarch:
                    case EmailOptIn.MonthlyBalanceReportEmailApril:
                    case EmailOptIn.MonthlyBalanceReportEmailMay:
                    case EmailOptIn.MonthlyBalanceReportEmailJune:
                    case EmailOptIn.MonthlyBalanceReportEmailJuly:
                    case EmailOptIn.MonthlyBalanceReportEmailAugust:
                    case EmailOptIn.MonthlyBalanceReportEmailSeptember:
                    case EmailOptIn.MonthlyBalanceReportEmailOctober:
                    case EmailOptIn.MonthlyBalanceReportEmailNovember:
                    case EmailOptIn.MonthlyBalanceReportEmailDecember:
                    {
                        emailOptInFilter = emailOptIn
                                .Where(x => x != EmailOptIn.MonthlyBalanceReportEmailJanuary.ToString() &&
                                            x != EmailOptIn.MonthlyBalanceReportEmailFebruary.ToString() &&
                                            x != EmailOptIn.MonthlyBalanceReportEmailMarch.ToString() &&
                                            x != EmailOptIn.MonthlyBalanceReportEmailApril.ToString() &&
                                            x != EmailOptIn.MonthlyBalanceReportEmailMay.ToString() &&
                                            x != EmailOptIn.MonthlyBalanceReportEmailJune.ToString() &&
                                            x != EmailOptIn.MonthlyBalanceReportEmailJuly.ToString() &&
                                            x != EmailOptIn.MonthlyBalanceReportEmailAugust.ToString() &&
                                            x != EmailOptIn.MonthlyBalanceReportEmailSeptember.ToString() &&
                                            x != EmailOptIn.MonthlyBalanceReportEmailOctober.ToString() &&
                                            x != EmailOptIn.MonthlyBalanceReportEmailNovember.ToString() &&
                                            x != EmailOptIn.MonthlyBalanceReportEmailDecember.ToString());
                        break;
                    }
                    case EmailOptIn.MonthlyCardBalanceReportEmailJanuary:
                    case EmailOptIn.MonthlyCardBalanceReportEmailFebruary:
                    case EmailOptIn.MonthlyCardBalanceReportEmailMarch:
                    case EmailOptIn.MonthlyCardBalanceReportEmailApril:
                    case EmailOptIn.MonthlyCardBalanceReportEmailMay:
                    case EmailOptIn.MonthlyCardBalanceReportEmailJune:
                    case EmailOptIn.MonthlyCardBalanceReportEmailJuly:
                    case EmailOptIn.MonthlyCardBalanceReportEmailAugust:
                    case EmailOptIn.MonthlyCardBalanceReportEmailSeptember:
                    case EmailOptIn.MonthlyCardBalanceReportEmailOctober:
                    case EmailOptIn.MonthlyCardBalanceReportEmailNovember:
                    case EmailOptIn.MonthlyCardBalanceReportEmailDecember:
                    {
                        emailOptInFilter = emailOptIn
                                .Where(x => x != EmailOptIn.MonthlyCardBalanceReportEmailJanuary.ToString() &&
                                            x != EmailOptIn.MonthlyCardBalanceReportEmailFebruary.ToString() &&
                                            x != EmailOptIn.MonthlyCardBalanceReportEmailMarch.ToString() &&
                                            x != EmailOptIn.MonthlyCardBalanceReportEmailApril.ToString() &&
                                            x != EmailOptIn.MonthlyCardBalanceReportEmailMay.ToString() &&
                                            x != EmailOptIn.MonthlyCardBalanceReportEmailJune.ToString() &&
                                            x != EmailOptIn.MonthlyCardBalanceReportEmailJuly.ToString() &&
                                            x != EmailOptIn.MonthlyCardBalanceReportEmailAugust.ToString() &&
                                            x != EmailOptIn.MonthlyCardBalanceReportEmailSeptember.ToString() &&
                                            x != EmailOptIn.MonthlyCardBalanceReportEmailOctober.ToString() &&
                                            x != EmailOptIn.MonthlyCardBalanceReportEmailNovember.ToString() &&
                                            x != EmailOptIn.MonthlyCardBalanceReportEmailDecember.ToString());
                        break;
                    }
                }

                user.EmailOptIn = string.Join(';', emailOptInFilter);
                await db.SaveChangesAsync();
            }

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
