using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Requests.Queries.Cards;
using Sig.App.Backend.Services.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Queries.Beneficiaries
{
    public class ExportCardsList : IRequestHandler<ExportCardsList.Input, string>
    {
        private readonly AppDbContext db;
        private readonly IMediator mediator;

        public ExportCardsList(AppDbContext db, IMediator mediator)
        {
            this.mediator = mediator;
            this.db = db;
        }

        public async Task<string> Handle(Input request, CancellationToken cancellationToken)
        {
            var projectId = request.ProjectId.LongIdentifierForType<Project>();

            var project = await db.Projects.FirstOrDefaultAsync(x => x.Id == projectId);
            if (project == null)
            {
                throw new ProjectNotFoundException();
            }

            var cards = await db.Cards.Include(x => x.Beneficiary).ThenInclude(x => x.Subscriptions).ThenInclude(x => x.Subscription)
                .Include(x => x.Transactions)
                .Include(x => x.Funds).ThenInclude(x => x.ProductGroup)
                .Where(x => x.ProjectId == projectId)
                .ToListAsync();
            
            var cardBalanceReports = new List<CardBalanceReport>();

            foreach (var card in cards)
            {
                cardBalanceReports.Add(new CardBalanceReport()
                {
                    Card = card,
                    Total = card.TotalFund()
                });
            }

            var generator = new ExcelGenerator();
            generator.AddDataWorksheet("Rapport des cartes mensuel", cardBalanceReports)
                .Column("Numéro", x => x.Card.CardNumber)
                .Column("ID de carte de programme", x => x.Card.ProgramCardId)
                .Column("Status", x => CardHelper.GetCardStatus(x.Card.Status))
                .Column("Fonds carte cadeau", x => MoneyHelper.GetMoneyFormat(x.Card.LoyaltyFund(), MoneyHelper.EN))
                .Column("Fonds d'abonnement", x => MoneyHelper.GetMoneyFormat(x.Card.TotalSubscriptionFund(), MoneyHelper.EN))
                .Column("Total", x => MoneyHelper.GetMoneyFormat(x.Total, MoneyHelper.EN))
                .Column("Date d'expiration des fonds en fonction de l'utilisation", x =>
                {
                    if (x.Card.Beneficiary != null && x.Card.Beneficiary.Subscriptions.Any(x => x.Subscription.IsSubscriptionPaymentBasedCardUsage))
                    {
                        var nearestExpirationDate = TransactionHelper.GetNearestExpirationDate(x.Card.Transactions);
                        if (nearestExpirationDate != null)
                        {
                            return $"{nearestExpirationDate.Value.ToString(DateFormats.RegularExport)}";
                        }
                    }

                    return "";
                });

            var result = await mediator.Send(new SaveTemporaryFile.Command
            {
                File = new FileInfos
                {
                    Content = generator.Render(),
                    ContentType = ContentTypes.Xlsx,
                    FileName = $"Cards_{project.Name}.xlsx"
                }
            });

            return result.FileUrl;
        }

        public class Input : IRequest<string>
        {
            public Id ProjectId { get; set; }
        }

        public class Payload
        {
            public string FileUrl { get; set; }
        }

        public class ProjectNotFoundException : RequestValidationException { }
    }
}
