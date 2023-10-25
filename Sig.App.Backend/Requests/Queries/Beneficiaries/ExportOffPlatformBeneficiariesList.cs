using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Services.Files;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.Services.Beneficiaries;
using static Sig.App.Backend.Helpers.ExcelGenerator;
using System;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Migrations;
using OffPlatformBeneficiary = Sig.App.Backend.DbModel.Entities.Beneficiaries.OffPlatformBeneficiary;
using Organization = Sig.App.Backend.DbModel.Entities.Organizations.Organization;

namespace Sig.App.Backend.Requests.Commands.Queries.Beneficiaries
{
    public class ExportOffPlatformBeneficiariesList : IRequestHandler<ExportOffPlatformBeneficiariesList.Input, string>
    {
        private readonly AppDbContext db;
        private readonly IMediator mediator;
        private readonly IBeneficiaryService beneficiaryService;

        public ExportOffPlatformBeneficiariesList(AppDbContext db, IMediator mediator, IBeneficiaryService beneficiaryService)
        {
            this.mediator = mediator;
            this.db = db;
            this.beneficiaryService = beneficiaryService;
        }

        public async Task<string> Handle(Input request, CancellationToken cancellationToken)
        {
            var currentUserCanSeeAllBeneficiaryInfo = await beneficiaryService.CurrentUserCanSeeAllBeneficiaryInfo();

            IQueryable<Beneficiary> query = db.Beneficiaries
                .Where(x => (x is OffPlatformBeneficiary))
                .Include(x => x.Organization)
                .Include(x => (x as OffPlatformBeneficiary).PaymentFunds)
                .Include(x => x.Card).ThenInclude(x => x.Funds).ThenInclude(x => x.ProductGroup)
                .Include(x => x.Card).ThenInclude(x => x.Transactions);
            
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
                .Select(x => x as OffPlatformBeneficiary).ToListAsync(cancellationToken: cancellationToken);
            
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

            dataWorksheet.Column("Solde total/Total balance", x => x.Card != null ? x.Card.TotalFund() : "");
            dataWorksheet.Column("Solde programme/Program balance", x => x.Card != null ? x.Card.TotalSubscriptionFund() : "");

            foreach (var productGroup in productGroups)
            {
                if (productGroup.Name != ProductGroupType.LOYALTY)
                {
                    dataWorksheet.Column("Solde " + productGroup.Name, x => {
                        if (x.Card != null)
                        {
                            return x.Card.Funds.FirstOrDefault(x => x.ProductGroupId == productGroup.Id)?.Amount;
                        }
                        return null;
                    });
                }
            }

            dataWorksheet.Column("Solde carte-cadeau/Gift card balance", x => x.Card != null ? x.Card.LoyaltyFund() : "");
            dataWorksheet.Column("Dépenses totales/Total expenses", x =>
            {
                if (x.Card != null)
                {
                    var transactions = db.Transactions.Where(y => y.BeneficiaryId == x.Id).ToList();
                    return transactions.Where(x => x.GetType() == typeof(PaymentTransaction)).Sum(x => x.Amount);
                }
                else
                {
                    return "";
                }
            });
            foreach (var productGroup in productGroups)
            {
                if (productGroup.Name != ProductGroupType.LOYALTY)
                {
                    dataWorksheet.Column("Dépenses/Expenses " + productGroup.Name, x => {
                        var transactions = db.PaymentTransactionProductGroups.Where(y => y.PaymentTransaction.BeneficiaryId == x.Id).ToList();
                        if (transactions.Where(x => x.ProductGroupId == productGroup.Id).Any())
                        {
                            return transactions.Where(x => x.ProductGroupId == productGroup.Id).Sum(x => x.Amount);
                        }
                        return "";
                    });
                }
            }

            dataWorksheet.Column("Date début/Start Date", x =>
            {
                if (x.StartDate.HasValue)
                {
                    var startDate = TimeZoneInfo.ConvertTime(x.StartDate.Value, TimeZoneInfo.Utc).ToString(DateFormats.RegularExport);
                    return startDate;
                }
                return "";
            });
            dataWorksheet.Column("Date de fin/End date", x =>
            {
                if (x.EndDate.HasValue)
                {
                    var endDate = TimeZoneInfo.ConvertTime(x.EndDate.Value, TimeZoneInfo.Utc).ToString(DateFormats.RegularExport);
                    return endDate;
                }
                return "";
            });
            dataWorksheet.Column("Fréquence versement/Payment Frequency", x => {
                switch (x.MonthlyPaymentMoment)
                {
                    case SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth:
                        return "mensuel";
                    case SubscriptionMonthlyPaymentMoment.FirstAndFifteenthDayOfTheMonth:
                        return "bi-mensuel";
                    case SubscriptionMonthlyPaymentMoment.FirstDayOfTheWeek:
                        return "hebdomadaire";
                }
                return "";
            });

            foreach (var productGroup in productGroups)
            {
                if (productGroup.Name != ProductGroupType.LOYALTY)
                {
                    dataWorksheet.Column("Montant/Amount - " + productGroup.Name, x => {
                        return x.PaymentFunds.FirstOrDefault(x => x.ProductGroupId == productGroup.Id)?.Amount;
                    });
                }
            }

            dataWorksheet.Column("Statut/Status", x => x.IsActive ? "Actif/Active" : "Inactif/Inactive");
            dataWorksheet.Column("ID carte/Card ID", x => x.Card != null ? x.CardId : "");
            dataWorksheet.Column("Numéro carte/Card Number", x => x.Card != null ? x.Card.CardNumber : "");
            dataWorksheet.Column("Organisme/Organization", x => x.Organization != null ? x.Organization.Name : "");
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

        public class Input : IRequest<string>
        {
            public Id Id { get; set; }
            public string TimeZoneId { get; set; }
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
