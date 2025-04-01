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
using Sig.App.Backend.Services.Beneficiaries;
using Organization = Sig.App.Backend.DbModel.Entities.Organizations.Organization;
using Sig.App.Backend.Gql.Schema.Enums;
using System.Globalization;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Requests.Queries.Beneficiaries;
using Sig.App.Backend.Utilities.Sorting;
using Sig.App.Backend.DbModel.Entities.Projects;

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

            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            IQueryable<Beneficiary> query = db.Beneficiaries
                .Include(x => x.Organization)
                .Include(x => x.BeneficiaryType)
                .Include(x => x.Card).ThenInclude(x => x.Transactions)
                .Include(x => x.Card).ThenInclude(x => x.Funds).ThenInclude(x => x.ProductGroup)
                .Include(x => x.Subscriptions).ThenInclude(x => x.Subscription).AsNoTracking();

            if (request.Subscriptions != null)
            {
                var withoutSubscription = request.WithoutSubscription?.Value ?? false;
                query = query.Where(x => (withoutSubscription && x.Subscriptions.Count == 0) || x.Subscriptions.Any(y => request.Subscriptions.Contains(y.SubscriptionId)));
            }
            else if (request.WithoutSubscription.IsSet())
            {
                if (request.WithoutSubscription.Value)
                {
                    query = query.Where(x => x.Subscriptions.Count == 0);
                }
                else
                {
                    query = query.Where(x => x.Subscriptions.Count > 0);
                }
            }

            if (request.WithoutSpecificSubscriptions != null)
            {
                query = query.Where(x => !x.Subscriptions.Any(y => request.WithoutSpecificSubscriptions.Contains(y.SubscriptionId)));
            }

            if (request.Categories != null)
            {
                query = query.Where(x => request.Categories.Contains(x.BeneficiaryTypeId.GetValueOrDefault()));
            }

            if (request.WithoutSpecificCategories != null)
            {
                query = query.Where(x => !request.WithoutSpecificCategories.Contains(x.BeneficiaryTypeId.GetValueOrDefault()));
            }

            if (request.WithCard.IsSet())
            {
                if (request.WithCard.Value)
                {
                    query = query.Where(x => x.CardId != null);
                }
                else
                {
                    query = query.Where(x => x.CardId == null);
                }
            }

            if (request.WithConflictPayment.IsSet())
            {
                if (request.WithConflictPayment.Value)
                {
                    query = query.Where(x => x.Subscriptions.Any(y => y.BeneficiaryType != x.BeneficiaryType && y.Subscription.EndDate > today));
                }
                else
                {
                    query = query.Where(x => !x.Subscriptions.Any(y => y.BeneficiaryType != x.BeneficiaryType && y.Subscription.EndDate > today));
                }
            }

            if (request.WithCardDisabled.IsSet())
            {
                if (request.WithCardDisabled.Value)
                {
                    query = query.Where(x => x.Card.IsDisabled == request.WithCardDisabled.Value);
                }
                else
                {
                    query = query.Where(x => x.Card.IsDisabled == request.WithCardDisabled.Value || x.Card == null);
                }
            }

            if (request.Status != null)
            {
                var isActive = request.Status.Contains(BeneficiaryStatus.Active);
                var isInactive = request.Status.Contains(BeneficiaryStatus.Inactive);

                query = query.Where(x => ((x as OffPlatformBeneficiary).IsActive == true && isActive) || ((x as OffPlatformBeneficiary).IsActive == false && isInactive));
            }

            if (request.SearchText.IsSet() && !string.IsNullOrEmpty(request.SearchText.Value))
            {
                var searchText = request.SearchText.Value.Split(' ').AsEnumerable();

                foreach (var text in searchText)
                {
                    if (currentUserCanSeeAllBeneficiaryInfo)
                    {
                        query = query.Where(x => x.ID1.Contains(text) || x.ID2.Contains(text) || x.Email.Contains(text) || x.Firstname.Contains(text) || x.Lastname.Contains(text) || (x.Card != null && x.Card.CardNumber.Contains(text) || x.Card.CardNumber.Replace("-", string.Empty).Contains(text) || x.Card.ProgramCardId.ToString().Contains(text)));
                    }
                    else
                    {
                        query = query.Where(x => x.ID1.Contains(text) || x.ID2.Contains(text) || (x.Card != null && x.Card.CardNumber.Contains(text) || x.Card.CardNumber.Replace("-", "").Contains(text) || x.Card.ProgramCardId.ToString().Contains(text)));
                    }
                }
            }

            String fileName;
            if (request.Id.IsIdentifierForType<Organization>())
            {
                var organizationId = request.Id.LongIdentifierForType<Organization>();
                var organization = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);
                if (organization == null) throw new OrganizationNotFoundException();
                query = query.Where(x => x.OrganizationId == organizationId);
                fileName = organization.Name.Replace(" ", "");
            }
            else if (request.Id.IsIdentifierForType<Project>())
            {
                var projectId = request.Id.LongIdentifierForType<Project>();
                var project = await db.Projects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);
                if (project == null) throw new ProjectNotFoundException();
                query = query.Where(x => x.Organization.ProjectId == projectId);
                fileName = project.Name.Replace(" ", "");
            }
            else
            {
                throw new MustSpecifyOrganizationOrProjectException();
            }

            var sorted = Sort(query, request.Sort?.Field ?? BeneficiarySort.Default);
            var beneficiaries = await sorted.ToListAsync(cancellationToken: cancellationToken);
            
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
                    var transactions = x.Card.Transactions;
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

        private static IOrderedQueryable<Beneficiary> Sort(IQueryable<Beneficiary> query, BeneficiarySort sort)
        {
            switch (sort)
            {
                case BeneficiarySort.SortOrder:
                case BeneficiarySort.Default:
                    return query
                        .SortBy(x => x.SortOrder, SortOrder.Asc);
                case BeneficiarySort.ID1:
                    return query
                        .SortBy(x => x.ID1, SortOrder.Asc);
                case BeneficiarySort.LastName:
                    return query
                        .SortBy(x => x.Lastname, SortOrder.Asc);
                case BeneficiarySort.ByFundAvailableOnCard:
                    return query
                        .SortBy(x => x.Card != null ? x.Card.Funds.Where(x => x.ProductGroup.Name != ProductGroupType.LOYALTY).Sum(x => x.Amount) : 0, SortOrder.Desc);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public class Input : IRequest<string>
        {
            public Id Id { get; set; }
            public string TimeZoneId { get; set; }
            public Language Language { get; set; }
            public Sort<BeneficiarySort> Sort { get; set; }
            public Maybe<bool> WithoutSubscription { get; set; }
            public IEnumerable<long> Subscriptions { get; set; }
            public IEnumerable<long> WithoutSpecificSubscriptions { get; set; }
            public IEnumerable<long> Categories { get; set; }
            public IEnumerable<long> WithoutSpecificCategories { get; set; }
            public IEnumerable<BeneficiaryStatus> Status { get; set; }
            public Maybe<bool> WithCard { get; set; }
            public Maybe<bool> WithConflictPayment { get; set; }
            public Maybe<string> SearchText { get; set; }
            public Maybe<bool> WithCardDisabled { get; set; }
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
