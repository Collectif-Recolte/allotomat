using System;
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
                var emailOptIn = user.EmailOptIn.Split(';')
                    .Where(x => Enum.TryParse<EmailOptIn>(x, out _))
                    .Select(Enum.Parse<EmailOptIn>);

                switch (request.EmailType)
                {
                    case EmailOptIn.CreatedCardPdfEmail:
                    {
                        emailOptIn = emailOptIn.Except([EmailOptIn.CreatedCardPdfEmail]);
                        break;
                    }
                    case EmailOptIn.SubscriptionExpirationEmail:
                    {
                        emailOptIn = emailOptIn.Except([EmailOptIn.SubscriptionExpirationEmail]);
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
                        emailOptIn = emailOptIn.Except([
                            EmailOptIn.MonthlyBalanceReportEmailJanuary,
                            EmailOptIn.MonthlyBalanceReportEmailFebruary,
                            EmailOptIn.MonthlyBalanceReportEmailMarch,
                            EmailOptIn.MonthlyBalanceReportEmailApril,
                            EmailOptIn.MonthlyBalanceReportEmailMay,
                            EmailOptIn.MonthlyBalanceReportEmailJune,
                            EmailOptIn.MonthlyBalanceReportEmailJuly,
                            EmailOptIn.MonthlyBalanceReportEmailAugust,
                            EmailOptIn.MonthlyBalanceReportEmailSeptember,
                            EmailOptIn.MonthlyBalanceReportEmailOctober,
                            EmailOptIn.MonthlyBalanceReportEmailNovember,
                            EmailOptIn.MonthlyBalanceReportEmailDecember
                        ]);
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
                        emailOptIn = emailOptIn.Except([
                            EmailOptIn.MonthlyCardBalanceReportEmailJanuary,
                            EmailOptIn.MonthlyCardBalanceReportEmailFebruary,
                            EmailOptIn.MonthlyCardBalanceReportEmailMarch,
                            EmailOptIn.MonthlyCardBalanceReportEmailApril,
                            EmailOptIn.MonthlyCardBalanceReportEmailMay,
                            EmailOptIn.MonthlyCardBalanceReportEmailJune,
                            EmailOptIn.MonthlyCardBalanceReportEmailJuly,
                            EmailOptIn.MonthlyCardBalanceReportEmailAugust,
                            EmailOptIn.MonthlyCardBalanceReportEmailSeptember,
                            EmailOptIn.MonthlyCardBalanceReportEmailOctober,
                            EmailOptIn.MonthlyCardBalanceReportEmailNovember,
                            EmailOptIn.MonthlyCardBalanceReportEmailDecember
                        ]);
                        break;
                    }
                }

                user.EmailOptIn = string.Join(';', emailOptIn);
                await db.SaveChangesAsync(cancellationToken);
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
