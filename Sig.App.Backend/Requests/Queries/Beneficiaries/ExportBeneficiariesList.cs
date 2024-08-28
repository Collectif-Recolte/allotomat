using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Services.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Migrations;
using Sig.App.Backend.Services.Beneficiaries;
using Organization = Sig.App.Backend.DbModel.Entities.Organizations.Organization;
using Sig.App.Backend.Gql.Schema.Enums;
using System.Globalization;

namespace Sig.App.Backend.Requests.Commands.Queries.Beneficiaries
{
    public class ExportBeneficiariesList : IRequestHandler<ExportBeneficiariesList.Input, string>
    {
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly IMediator mediator;
        private readonly IBeneficiaryService beneficiaryService;

        public ExportBeneficiariesList(AppDbContext db, IClock clock, IMediator mediator, IBeneficiaryService beneficiaryService)
        {
            this.mediator = mediator;
            this.clock = clock;
            this.db = db;
            this.beneficiaryService = beneficiaryService;
        }

        public async Task<string> Handle(Input request, CancellationToken cancellationToken)
        {
            var currentUserCanSeeAllBeneficiaryInfo = await beneficiaryService.CurrentUserCanSeeAllBeneficiaryInfo();

            IQueryable<Beneficiary> query = db.Beneficiaries
                .Include(x => x.Organization)
                .Include(x => x.BeneficiaryType)
                .Include(x => x.Card).ThenInclude(x => x.Transactions)
                .Include(x => x.Card).ThenInclude(x => x.Funds).ThenInclude(x => x.ProductGroup)
                .Include(x => x.Subscriptions).ThenInclude(x => x.Subscription);
            
            String fileName;
            if (request.Id.IsIdentifierForType<Organization>())
            {
                var organizationId = request.Id.LongIdentifierForType<Organization>();
                var organization = await db.Organizations.FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);
                if (organization == null) throw new OrganizationNotFoundException();
                query = query.Where(x => x.OrganizationId == organizationId);
                fileName = organization.Name.Replace(" ", "");
            }
            else if (request.Id.IsIdentifierForType<Project>())
            {
                var projectId = request.Id.LongIdentifierForType<Project>();
                var project = await db.Projects.FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);
                if (project == null) throw new ProjectNotFoundException();
                query = query.Where(x => x.Organization.ProjectId == projectId);
                fileName = project.Name.Replace(" ", "");
            }
            else
            {
                throw new MustSpecifyOrganizationOrProjectException();
            }
            
            var beneficiaries = await query.OrderBy(x => x.SortOrder)
                .ToListAsync(cancellationToken: cancellationToken);
            
            var productGroups = beneficiaries.Where(x => x.Card != null).SelectMany(x => x.Card.Funds).Select(x => x.ProductGroup).DistinctBy(x => x.Id);

            var generator = new ExcelGenerator();
            var dataWorksheet = generator.AddDataWorksheet("Liste des participants", beneficiaries);
            dataWorksheet.Column("Id 1", x => x.ID1);
            dataWorksheet.Column("Id 2", x => x.ID2);
            
            if (currentUserCanSeeAllBeneficiaryInfo)
            {
                dataWorksheet.Column("Prénom/Firstname", x => x.Firstname);
                dataWorksheet.Column("Nom de famille/Lastname", x => x.Lastname);
                dataWorksheet.Column("Courriel/Email", x => x.Email);
                dataWorksheet.Column("Téléphone/Phone", x => x.Phone);
                dataWorksheet.Column("Adresse/Address", x => x.Address);
                dataWorksheet.Column("Code postal/Postal code", x => x.PostalCode);
                dataWorksheet.Column("Notes/Briefing", x => x.Notes);
            }
            
            dataWorksheet.Column("Catégorie/Category", x => x.BeneficiaryType.GetKeys().First());
            dataWorksheet.Column("Solde total/Total balance", x => x.Card != null ? x.Card.TotalFund().ToString("C", request.Language == Language.French ? CultureInfo.CreateSpecificCulture("fr-CA") : CultureInfo.CreateSpecificCulture("en-CA")) : "");
            dataWorksheet.Column("Solde abonnement/Subscription balance", x => x.Card != null ? x.Card.TotalSubscriptionFund().ToString("C", request.Language == Language.French ? CultureInfo.CreateSpecificCulture("fr-CA") : CultureInfo.CreateSpecificCulture("en-CA")) : "");

            foreach (var productGroup in productGroups)
            {
                if (productGroup.Name != ProductGroupType.LOYALTY)
                {
                    dataWorksheet.Column("Montant/Amount - " + productGroup.Name, x => {
                        if (x.Card != null)
                        {
                            return x.Card.Funds.FirstOrDefault(x => x.ProductGroupId == productGroup.Id)?.Amount.ToString("C", request.Language == Language.French ? CultureInfo.CreateSpecificCulture("fr-CA") : CultureInfo.CreateSpecificCulture("en-CA"));
                        }
                        return null;
                    });
                }
            }

            dataWorksheet.Column("Solde carte-cadeau/Gift card balance", x => x.Card != null ? x.Card.LoyaltyFund().ToString("C", request.Language == Language.French ? CultureInfo.CreateSpecificCulture("fr-CA") : CultureInfo.CreateSpecificCulture("en-CA")) : "");
            dataWorksheet.Column("Dépenses/Expenses", x =>
            {
                if (x.Card != null)
                {
                    var transactions = db.Transactions.Where(y => y.BeneficiaryId == x.Id).ToList();
                    return transactions.Where(x => x.GetType() == typeof(PaymentTransaction)).Sum(x => x.Amount).ToString("C", request.Language == Language.French ? CultureInfo.CreateSpecificCulture("fr-CA") : CultureInfo.CreateSpecificCulture("en-CA"));
                }
                else
                {
                    return "";
                }
            });
            dataWorksheet.Column("ID carte/Card ID", x => x.Card != null ? x.Card.ProgramCardId : "");
            dataWorksheet.Column("Numéro carte/Card Number", x => x.Card != null ? x.Card.CardNumber.Replace('-', ' ') : "");
            dataWorksheet.Column("Abonnements/Subscriptions", x => GetActiveSubscriptions(x.Subscriptions));
            dataWorksheet.Column("Groupe/Group", x => x.Organization != null ? x.Organization.Name : "");
            dataWorksheet.Column("Dernier usage/Last use", x =>
            {
                if (x.Card != null && x.Card.Transactions.Count > 0)
                {
                    var transactions = x.Card.Transactions;
                    var paymentTransactions = transactions.Where(x => x.GetType() == typeof(PaymentTransaction));

                    if (paymentTransactions.Count() > 0)
                    {
                        var lastTransaction = paymentTransactions.OrderBy(x => x.CreatedAtUtc).Last();
                        return $"{TimeZoneInfo.ConvertTime(lastTransaction.CreatedAtUtc, TimeZoneInfo.Utc, TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId)).ToString(DateFormats.RegularWithTime)}";
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
                    FileName = $"Participants_{fileName}.xlsx"
                }
            });

            return result.FileUrl;
        }

        private string GetActiveSubscriptions(IList<SubscriptionBeneficiary> subscriptions)
        {
            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            return string.Join(";", subscriptions.Select(x => x.Subscription.Name).ToArray());
        }

        public class Input : IRequest<string>
        {
            public Id Id { get; set; }
            public string TimeZoneId { get; set; }
            public Language Language { get; set; }
        }

        public class Payload
        {
            public string FileUrl { get; set; }
        }

        public class OrganizationNotFoundException : RequestValidationException { }
        public class ProjectNotFoundException : RequestValidationException { }
        public class MustSpecifyOrganizationOrProjectException : RequestValidationException { }
    }
}
