using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Helpers;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sig.App.Backend.Constants;
using Sig.App.Backend.Services.Beneficiaries;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Services.Permission;
using Microsoft.AspNetCore.Identity;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.Services.Permission.Enums;
using Sig.App.Backend.Gql.Schema.Enums;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.CashRegisters;

namespace Sig.App.Backend.Services.Reports
{
    public class ReportService : IReportService
    {
        private readonly IAppUserContext ctx;
        private readonly AppDbContext db;
        private readonly IBeneficiaryService beneficiaryService;
        private readonly UserManager<AppUser> userManager;
        private readonly PermissionService permissionService;

        public ReportService(IAppUserContext ctx, AppDbContext db, UserManager<AppUser> userManager, IBeneficiaryService beneficiaryService, PermissionService permissionService)
        {
            this.ctx = ctx;
            this.db = db;
            this.userManager = userManager;
            this.beneficiaryService = beneficiaryService;
            this.permissionService = permissionService;
        }

        public async Task<Stream> GenerateTransactionReportForMarket(IReportForMarketInput request)
        {
            var longMarketId = request.MarketId.LongIdentifierForType<Market>();
            var startDate = request.StartDate.ToUniversalTime();
            var endDate = request.EndDate.ToUniversalTime();

            IQueryable<TransactionLog> query = db.TransactionLogs.Include(x => x.TransactionLogProductGroups).Where(x =>
                x.CreatedAtUtc > startDate && x.CreatedAtUtc < endDate && x.MarketId == longMarketId);

            var transactions = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
            var productGroupDictionary = transactions
                .SelectMany(x => x.TransactionLogProductGroups).GroupBy(x => x.ProductGroupId).Select(x => x.First())
                .ToDictionary(x => x.ProductGroupId, x => x.ProductGroupName);

            var generator = new ExcelGenerator();
            var dataWorksheet = generator.AddDataWorksheet("Rapport de transactions bruts", transactions);
            dataWorksheet.Column("Date/Heure/Date/Hour", x => $"{TimeZoneInfo.ConvertTime(x.CreatedAtUtc, TimeZoneInfo.Utc, TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId)).ToString(DateFormats.RegularWithTime)}");
            dataWorksheet.Column("Id unique de transaction/Transaction unique id", x => x.TransactionUniqueId);
            dataWorksheet.Column("Id 1 (participant)", x => x.BeneficiaryID1);
            dataWorksheet.Column("Id 2 (participant)", x => x.BeneficiaryID2);

            dataWorksheet.Column("Participant-e hors plateforme/Participant off platform", x => x.BeneficiaryIsOffPlatform ? "Oui/Yes" : "Non/No");
            dataWorksheet.Column("Type", GetOperationTypeText);
            dataWorksheet.Column("Montant total/Total amount", x => GetAmountText(x, request.Language));

            foreach (var productGroup in productGroupDictionary)
            {
                if (productGroup.Value != ProductGroupType.LOYALTY)
                {
                    dataWorksheet.Column("Montant/Amount - " + productGroup.Value, x => GetAmountByProductGroup(productGroup.Key, x, request.Language));
                }
            }

            dataWorksheet.Column("Id de la carte/Card id", x => x.CardProgramCardId);
            dataWorksheet.Column("Numéro de la carte/Card number", x => x.CardNumber != null ? x.CardNumber.Replace("-", " ") : "");
            dataWorksheet.Column("Caisse/Cash register", x => x.CashRegisterName);

            return generator.Render();
        }

        public async Task<Stream> GenerateTransactionReport(IReportInput request)
        {
            var currentUserCanSeeAllBeneficiaryInfo = await beneficiaryService.CurrentUserCanSeeAllBeneficiaryInfo();
            var globalPermissions = await permissionService.GetGlobalPermissions(ctx.CurrentUser);
            var longProjectId = request.ProjectId.LongIdentifierForType<Project>();
            var startDate = request.StartDate.ToUniversalTime();
            var endDate = request.EndDate.ToUniversalTime();

            IQueryable<TransactionLog> query = db.TransactionLogs.Include(x => x.TransactionLogProductGroups).Where(x =>
                x.CreatedAtUtc > startDate && x.CreatedAtUtc < endDate && x.ProjectId == longProjectId);

            if (!globalPermissions.Contains(GlobalPermission.ManageOrganizations))
            {
                var user = await db.Users
                .Where(c => c.Id == ctx.CurrentUserId)
                .FirstAsync();

                var existingClaims = await userManager.GetClaimsAsync(user);
                var existingOrganizationsClaims = existingClaims.Where(x => x.Type == AppClaimTypes.OrganizationManagerOf).Select(x => x.Value).FirstOrDefault();
                query = query.Where(x => x.OrganizationId.ToString() == existingOrganizationsClaims);
            }
            else if(request.Organizations?.Any() ?? false)
            {
                var organizationsLongIdentifiers = request.Organizations.Select(x => x.LongIdentifierForType<Organization>());
                query = query.Where(x => organizationsLongIdentifiers.Contains(x.OrganizationId.GetValueOrDefault()));
            }

            if (request.Subscriptions?.Any() ?? false)
            {
                var withoutSubscription = request.WithoutSubscription?.Value ?? false;
                var subscriptionLongIdentifiers = request.Subscriptions.Select(x => x.LongIdentifierForType<Subscription>());
                query = query.Where(x => (withoutSubscription && !x.SubscriptionId.HasValue) || subscriptionLongIdentifiers.Contains(x.SubscriptionId.GetValueOrDefault()));
            }
            else if (request.WithoutSubscription.IsSet() && request.WithoutSubscription.Value)
            {
                query = query.Where(x => !x.SubscriptionId.HasValue);
            }

            if (request.Categories?.Any() ?? false)
            {
                var categoriesLongIdentifiers = request.Categories.Select(x => x.LongIdentifierForType<BeneficiaryType>());
                query = query.Where(x => categoriesLongIdentifiers.Contains(x.BeneficiaryTypeId.GetValueOrDefault()));
            }

            if (request.Markets?.Any() ?? false)
            {
                var marketsLongIdentifiers = request.Markets.Select(x => x.LongIdentifierForType<Market>());
                query = query.Where(x => marketsLongIdentifiers.Contains(x.MarketId.GetValueOrDefault()));
            }

            if (request.CashRegisters?.Any() ?? false)
            {
                var cashRegistersLongIdentifiers = request.CashRegisters.Select(x => x.LongIdentifierForType<CashRegister>());
                query = query.Where(x => cashRegistersLongIdentifiers.Contains(x.CashRegisterId.GetValueOrDefault()));
            }

            if (request.TransactionTypes?.Any() ?? false)
            {
                var transactionLogDiscriminators =
                    request.TransactionTypes.Select(x => Enum.Parse(typeof(TransactionLogDiscriminator), x));
                query = query.Where(x => transactionLogDiscriminators.Contains(x.Discriminator));
            }

            if (request.SearchText.IsSet() && !string.IsNullOrEmpty(request.SearchText.Value))
            {
                var searchText = request.SearchText.Value.Split(' ').AsEnumerable();
                foreach (var text in searchText)
                {
                    if (currentUserCanSeeAllBeneficiaryInfo)
                    {
                        query = query.Where(x =>
                            x.BeneficiaryID1.Contains(text) || x.BeneficiaryID1.Contains(text) ||
                            x.BeneficiaryEmail.Contains(text) || x.BeneficiaryFirstname.Contains(text) ||
                            x.BeneficiaryLastname.Contains(text));
                    }
                    else
                    {
                        query = query.Where(x => x.BeneficiaryID1.Contains(text) || x.BeneficiaryID2.Contains(text));
                    }
                }
            }

            var transactions = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync();
            var productGroupDictionary = transactions
                .SelectMany(x => x.TransactionLogProductGroups).GroupBy(x => x.ProductGroupId).Select(x => x.First())
                .ToDictionary(x => x.ProductGroupId, x => x.ProductGroupName);

            var generator = new ExcelGenerator();
            var dataWorksheet = generator.AddDataWorksheet("Rapport de transactions bruts", transactions);
            dataWorksheet.Column("Date/Heure/Date/Hour", x => $"{TimeZoneInfo.ConvertTime(x.CreatedAtUtc, TimeZoneInfo.Utc, TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId)).ToString(DateFormats.RegularWithTime)}");
            dataWorksheet.Column("Id unique de transaction/Transaction unique id", x => x.TransactionUniqueId);
            dataWorksheet.Column("Id 1 (participant)", x => x.BeneficiaryID1);
            dataWorksheet.Column("Id 2 (participant)", x => x.BeneficiaryID2);

            if (currentUserCanSeeAllBeneficiaryInfo)
            {
                dataWorksheet.Column("Participant-e/Participant", GetParticipantName);
                dataWorksheet.Column("Courriel participant-e/Participant email", x => x.BeneficiaryEmail);
                dataWorksheet.Column("Téléphone participant-e/Participant phone", x => x.BeneficiaryPhone);
            }

            dataWorksheet.Column("Participant-e hors plateforme/Participant off platform", x => x.BeneficiaryIsOffPlatform ? "Oui/Yes" : "Non/No");
            dataWorksheet.Column("Type", GetOperationTypeText);
            dataWorksheet.Column("Marché/Market", x => x.MarketName);
            dataWorksheet.Column("Montant total/Total amount", x => GetAmountText(x, request.Language));

            foreach (var productGroup in productGroupDictionary)
            {
                if (productGroup.Value != ProductGroupType.LOYALTY)
                {
                    dataWorksheet.Column("Montant/Amount - " + productGroup.Value, x => GetAmountByProductGroup(productGroup.Key, x, request.Language));
                }
            }

            dataWorksheet.Column("Id de la carte/Card id", x => x.CardProgramCardId);
            dataWorksheet.Column("Numéro de la carte/Card number", x => x.CardNumber != null ? x.CardNumber.Replace('-', ' ') : "");
            dataWorksheet.Column(
                "Transfert de fond depuis l'id de carte/Transferred fund from card id",
                x => x.FundTransferredFromProgramCardId);
            dataWorksheet.Column(
                "Transfert de fond depuis le numéro de carte/Transferred fund from card number",
                x => x.FundTransferredFromCardNumber != null ? x.FundTransferredFromCardNumber.Replace('-', ' ') : "");
            dataWorksheet.Column("Groupe/Group", x => x.OrganizationName);
            dataWorksheet.Column("Abonnement/Subscription", x => x.SubscriptionName);
            dataWorksheet.Column("Initiateur transaction/Transaction initiator", GetTransactionInitiatorName);
            dataWorksheet.Column("Courriel initiateur transaction/Transaction initiator email", x => x.TransactionInitiatorEmail);
            dataWorksheet.Column("Caisse/Cash register", x => x.CashRegisterName);

            return generator.Render();
        }

        private string GetParticipantName(TransactionLog transactionLog)
        {
            return $"{transactionLog.BeneficiaryFirstname} {transactionLog.BeneficiaryLastname}";
        }
        
        private string GetTransactionInitiatorName(TransactionLog transactionLog)
        {
            return $"{transactionLog.TransactionInitiatorFirstname} {transactionLog.TransactionInitiatorLastname}";
        }

        private string GetOperationTypeText(TransactionLog transactionLog)
        {
            switch (transactionLog.Discriminator)
            {
                case TransactionLogDiscriminator.ExpireFundTransactionLog:
                    return "Expiration des fonds/Expiry of funds";
                case TransactionLogDiscriminator.LoyaltyAddingFundTransactionLog:
                    return "Carte-cadeau/Gift card";
                case TransactionLogDiscriminator.ManuallyAddingFundTransactionLog:
                    return "Fond ajouté manuellement/Manually added fund";
                case TransactionLogDiscriminator.OffPlatformAddingFundTransactionLog:
                    return "Ajout de fond (participant hors plateforme)/Adding fund (off-platform participant)";
                case TransactionLogDiscriminator.SubscriptionAddingFundTransactionLog:
                    return "Abonnement/Subscription";
                case TransactionLogDiscriminator.PaymentTransactionLog:
                    return "Paiement/Payment";
                case TransactionLogDiscriminator.TransferFundTransactionLog:
                    return "Transfert de fond/Fund transfer";
                case TransactionLogDiscriminator.RefundBudgetAllowanceFromUnassignedCardTransactionLog:
                    return $"Remboursement d'enveloppe, carte désassignée au participant/Budget allowance refund, card unassigned from participant";
                case TransactionLogDiscriminator.RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog:
                    return $"Remboursement d'enveloppe, participant retiré de l'abonnement/Budget allowance refund, participant removed from subscription";
                case TransactionLogDiscriminator.RefundBudgetAllowanceFromNoCardWhenAddingFundTransactionLog:
                    return $"Remboursement d'enveloppe, participant sans carte lors de l'ajout de fond automatique/Budget allowance refund, participant had no cards when automatically adding fund";
                case TransactionLogDiscriminator.RefundPaymentTransactionLog:
                    return $"Remboursement d'un paiement/Refund of a payment";
                default:
                    return "Type inconnu/Unknown type";
            }
        }

        private string GetAmountText(TransactionLog transactionLog, Language language)
        {
            var amount = transactionLog.TotalAmount.ToString("C", language == Language.French ? CultureInfo.CreateSpecificCulture("fr-CA") : CultureInfo.CreateSpecificCulture("en-CA"));
            if (transactionLog.Discriminator == TransactionLogDiscriminator.ExpireFundTransactionLog || transactionLog.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog)
            {
                return $"-{amount}";
            }

            return amount;
        }

        private string GetAmountByProductGroup(long productGroupId, TransactionLog transactionLog, Language language)
        {
            var transactionByProduct = transactionLog.TransactionLogProductGroups.FirstOrDefault(x => x.ProductGroupId == productGroupId);
            if (transactionByProduct != null)
            {
                return transactionByProduct.Amount.ToString("C", language == Language.French ? CultureInfo.CreateSpecificCulture("fr-CA") : CultureInfo.CreateSpecificCulture("en-CA"));
            }
            
            return "";
        }
    }
}
