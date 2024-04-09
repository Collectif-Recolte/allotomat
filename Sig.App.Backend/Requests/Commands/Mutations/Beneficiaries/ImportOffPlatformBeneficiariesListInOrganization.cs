using System;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.Gql.Bases;

namespace Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries
{
    public class ImportOffPlatformBeneficiariesListInOrganization : IRequestHandler<ImportOffPlatformBeneficiariesListInOrganization.Input, ImportOffPlatformBeneficiariesListInOrganization.Payload>
    {
        private readonly ILogger<ImportOffPlatformBeneficiariesListInOrganization> logger;
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly IHttpContextAccessor httpContextAccessor;

        public ImportOffPlatformBeneficiariesListInOrganization(ILogger<ImportOffPlatformBeneficiariesListInOrganization> logger, AppDbContext db, IClock clock, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.db = db;
            this.clock = clock;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] ImportOffPlatformBeneficiariesListInOrganization({request.Items.Length})");
            var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            var currentUser = db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId);
            var today = clock.GetCurrentInstant().InUtc().ToDateTimeUtc();

            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations.Include(x => x.Project).FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null)
            {
                logger.LogWarning("[Mutation] ImportOffPlatformBeneficiariesListInOrganization - OrganizationNotFoundException");
                throw new OrganizationNotFoundException();
            }

            if (!organization.Project.AdministrationSubscriptionsOffPlatform)
            {
                logger.LogWarning("[Mutation] ImportOffPlatformBeneficiariesListInOrganization - ProjectDontAdministrateSubscriptionOffPlatformException");
                throw new ProjectDontAdministrateSubscriptionOffPlatformException();
            }

            var beneficiaries = new List<OffPlatformBeneficiary>();
            var sortOrder = 0;
            
            var currentBeneficiaries = await db.Beneficiaries
                .Where(x => (x is OffPlatformBeneficiary))
                .Include(x => (x as OffPlatformBeneficiary).PaymentFunds)
                .Include(x => x.Card).ThenInclude(x => x.Funds)
                .Include(x => x.Card).ThenInclude(x => x.Transactions).ThenInclude(x => (x as AddingFundTransaction).ProductGroup)
                .Where(x => x.OrganizationId == organization.Id).OrderBy(x => x.SortOrder)
                .Select(x => x as OffPlatformBeneficiary).ToListAsync();

            var isToday = false;

            foreach (var item in request.Items)
            {
                var beneficiary = currentBeneficiaries.Where(x => x.ID1 == item.Id1).FirstOrDefault();

                if (beneficiary == null) {
                    beneficiary = new OffPlatformBeneficiary()
                    {
                        ID1 = item.Id1,
                        Organization = organization,
                    };
                    db.Beneficiaries.Add(beneficiary);
                    logger.LogInformation($"[Mutation] ImportOffPlatformBeneficiariesListInOrganization - New off-platform beneficiary created {beneficiary.Firstname} {beneficiary.Lastname}");
                }

                beneficiary.Firstname = item.Firstname;
                beneficiary.Lastname = item.Lastname;
                beneficiary.Email = item.Email;
                beneficiary.Address = item.Address;
                beneficiary.Phone = item.Phone;
                beneficiary.Notes = item.Notes;
                beneficiary.SortOrder = sortOrder++;
                beneficiary.PostalCode = item.PostalCode;
                beneficiary.ID1 = item.Id1;
                beneficiary.ID2 = item.Id2;

                if (item.StartDate.IsSet())
                {
                    beneficiary.StartDate = item.StartDate.Value.AtMidnight().InUtc().ToDateTimeUtc();
                }
                else
                {
                    beneficiary.StartDate = new LocalDate(today.Year, today.Month, today.Day).AtMidnight().InUtc().ToDateTimeUtc();
                }

                isToday = beneficiary.StartDate.Value.Date == today.Date;

                if (item.EndDate.IsSet())
                {
                    beneficiary.EndDate = item.EndDate.Value.AtMidnight().InUtc().ToDateTimeUtc();
                }
                else
                {
                    beneficiary.EndDate = null;
                }

                beneficiary.MonthlyPaymentMoment = item.MonthlyPaymentMoment;

                beneficiary.IsActive = true;

                beneficiary.PaymentFunds = new List<PaymentFund>();
                foreach (var fund in item.Funds)
                {
                    var productGroup = db.ProductGroups.FirstOrDefault(x => x.Name == fund.ProductGroupName && x.ProjectId == organization.Project.Id);

                    if (productGroup == null)
                    {
                        logger.LogWarning($"[Mutation] ImportOffPlatformBeneficiariesListInOrganization - ProductGroupNotFoundException ({fund.ProductGroupName})");
                        throw new ProductGroupNotFoundException();
                    }

                    if (isToday && beneficiary.Card != null)
                    {
                        var cardFund = beneficiary.Card.Funds.FirstOrDefault(x => x.ProductGroupId == productGroup.Id);
                        
                        if (cardFund == null)
                        {
                            cardFund = new Fund()
                            {
                                Card = beneficiary.Card,
                                ProductGroup = productGroup
                            };
                            db.Funds.Add(cardFund);
                            logger.LogInformation($"[Mutation] ImportOffPlatformBeneficiariesListInOrganization - New fund created {beneficiary.Card.Id} - {productGroup.Name}");
                        }

                        var transactionUniqueId = TransactionHelper.CreateTransactionUniqueId();
                        var now = clock.GetCurrentInstant().InUtc().ToDateTimeUtc();
                        db.Transactions.Add(new OffPlatformAddingFundTransaction()
                        {
                            TransactionUniqueId = transactionUniqueId,
                            Card = beneficiary.Card,
                            Beneficiary = beneficiary,
                            OrganizationId = beneficiary.OrganizationId,
                            Amount = fund.Amount,
                            AvailableFund = fund.Amount,
                            CreatedAtUtc = now,
                            ExpirationDate = SubscriptionHelper.GetNextPaymentDateTime(clock, beneficiary.MonthlyPaymentMoment.Value),
                            ProductGroup = productGroup
                        });
                        
                        var transactionLogProductGroups = new List<TransactionLogProductGroup>()
                        {
                            new()
                            {
                                Amount = fund.Amount,
                                ProductGroupId = productGroup.Id,
                                ProductGroupName = productGroup.Name
                            }
                        };
                        
                        db.TransactionLogs.Add(new TransactionLog()
                        {
                            Discriminator = TransactionLogDiscriminator.OffPlatformAddingFundTransactionLog,
                            TransactionUniqueId = transactionUniqueId,
                            CreatedAtUtc = now,
                            TotalAmount = fund.Amount,
                            CardProgramCardId = beneficiary.Card.ProgramCardId,
                            CardNumber = beneficiary.Card.CardNumber,
                            BeneficiaryId = beneficiary.Id,
                            BeneficiaryID1 = beneficiary.ID1,
                            BeneficiaryID2 = beneficiary.ID2,
                            BeneficiaryFirstname = beneficiary.Firstname,
                            BeneficiaryLastname = beneficiary.Lastname,
                            BeneficiaryEmail = beneficiary.Email,
                            BeneficiaryPhone = beneficiary.Phone,
                            BeneficiaryIsOffPlatform = true,
                            BeneficiaryTypeId = beneficiary.BeneficiaryTypeId,
                            OrganizationId = beneficiary.OrganizationId,
                            OrganizationName = beneficiary.Organization.Name,
                            ProjectId = beneficiary.Organization.ProjectId,
                            ProjectName = beneficiary.Organization.Project.Name,
                            TransactionInitiatorId = currentUserId,
                            TransactionInitiatorEmail = currentUser?.Email,
                            TransactionInitiatorFirstname = currentUser?.Profile.FirstName,
                            TransactionInitiatorLastname = currentUser?.Profile.LastName,
                            TransactionLogProductGroups = transactionLogProductGroups
                        });

                        cardFund.Amount = fund.Amount;
                    }

                    beneficiary.PaymentFunds.Add(new PaymentFund()
                    {
                        Amount = fund.Amount,
                        Beneficiary = beneficiary,
                        ProductGroup = productGroup
                    });
                }

                beneficiaries.Add(beneficiary);

                if (beneficiary.EndDate == null || beneficiary.EndDate > today)
                {
                    currentBeneficiaries.Remove(beneficiary);
                }
            }

            foreach (var beneficiary in currentBeneficiaries)
            {
                beneficiary.SortOrder = sortOrder++;
                beneficiary.IsActive = false;
                beneficiary.MonthlyPaymentMoment = null;
                beneficiary.StartDate = null;
                beneficiary.EndDate = null;
                beneficiary.PaymentFunds = new List<PaymentFund>();

                if (beneficiary.Card != null)
                {
                    foreach (var fund in beneficiary.Card.Funds)
                    {
                        if (fund.ProductGroup.Name != ProductGroupType.LOYALTY)
                        {
                            fund.Amount = 0;
                        }
                    }

                    var addingFundTransactions = beneficiary.Card.Transactions
                        .Where(x => (x.GetType() == typeof(AddingFundTransaction) ||
                            x.GetType() == typeof(ManuallyAddingFundTransaction) ||
                            x.GetType() == typeof(OffPlatformAddingFundTransaction))
                            && (x as AddingFundTransaction).Status == FundTransactionStatus.Actived)
                        .Select(x => x as AddingFundTransaction).ToList();
                    
                    foreach (var transaction in addingFundTransactions)
                    {
                        var transactionUniqueId = TransactionHelper.CreateTransactionUniqueId();

                        var expireFundTransaction = new ExpireFundTransaction()
                        {
                            TransactionUniqueId = transactionUniqueId,
                            Beneficiary = beneficiary,
                            Organization = organization,
                            Amount = transaction.AvailableFund,
                            Card = transaction.Card,
                            CreatedAtUtc = today,
                            ProductGroupId = transaction.ProductGroupId
                        };
                        db.Transactions.Add(expireFundTransaction);
                        transaction.ExpireFundTransaction = expireFundTransaction;

                        transaction.AvailableFund = 0;
                        transaction.Status = FundTransactionStatus.Expired;
                        
                        var transactionLogProductGroups = new List<TransactionLogProductGroup>()
                        {
                            new()
                            {
                                Amount = transaction.Amount,
                                ProductGroupId = transaction.ProductGroupId,
                                ProductGroupName = transaction.ProductGroup.Name
                            }
                        };
                        
                        db.TransactionLogs.Add(new TransactionLog()
                        {
                            Discriminator = TransactionLogDiscriminator.ExpireFundTransactionLog,
                            TransactionUniqueId = transactionUniqueId,
                            CreatedAtUtc = today,
                            TotalAmount = transaction.AvailableFund,
                            CardProgramCardId = transaction.Card.ProgramCardId,
                            CardNumber = transaction.Card.CardNumber,
                            BeneficiaryId = transaction.BeneficiaryId,
                            BeneficiaryID1 = transaction.Beneficiary.ID1,
                            BeneficiaryID2 = transaction.Beneficiary.ID2,
                            BeneficiaryFirstname = transaction.Beneficiary.Firstname,
                            BeneficiaryLastname = transaction.Beneficiary.Lastname,
                            BeneficiaryEmail = transaction.Beneficiary.Email,
                            BeneficiaryPhone = transaction.Beneficiary.Phone,
                            BeneficiaryIsOffPlatform = transaction.Beneficiary is OffPlatformBeneficiary,
                            BeneficiaryTypeId = transaction.Beneficiary.BeneficiaryTypeId,
                            OrganizationId = transaction.Beneficiary.OrganizationId,
                            OrganizationName = transaction.Beneficiary.Organization.Name,
                            ProjectId = transaction.Beneficiary.Organization.ProjectId,
                            ProjectName = transaction.Beneficiary.Organization.Project.Name,
                            TransactionInitiatorId = currentUserId,
                            TransactionInitiatorEmail = currentUser?.Email,
                            TransactionInitiatorFirstname = currentUser?.Profile.FirstName,
                            TransactionInitiatorLastname = currentUser?.Profile.LastName,
                            TransactionLogProductGroups = transactionLogProductGroups
                        });
                    }
                }
            }

            await db.SaveChangesAsync(cancellationToken);

            return new Payload
            {
                Beneficiaries = beneficiaries.Select(x => new OffPlatformBeneficiaryGraphType(x))
            };
        }

        [MutationInput]
        public class Input : HaveOrganizationId, IRequest<Payload>
        {
            public OffPlatformBeneficiaryItem[] Items { get; set; }
        }

        [InputType]
        public class OffPlatformBeneficiaryItem
        {
            public string Firstname { get; set; }
            public string Lastname { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string Address { get; set; }
            public string Notes { get; set; }
            public string PostalCode { get; set; }
            public string Id1 { get; set; }
            public string Id2 { get; set; }
            public Maybe<LocalDate> StartDate { get; set; }
            public Maybe<LocalDate> EndDate { get; set; }
            public SubscriptionMonthlyPaymentMoment MonthlyPaymentMoment { get; set; }
            public List<FundType­> Funds { get; set; }
        }

        [InputType]
        public class FundType
        {
            public string ProductGroupName { get; set; }
            public decimal Amount { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public IEnumerable<OffPlatformBeneficiaryGraphType> Beneficiaries { get; set; }
        }

        public class OrganizationNotFoundException : RequestValidationException { }
        public class ProductGroupNotFoundException : RequestValidationException { }
        public class ProjectDontAdministrateSubscriptionOffPlatformException : RequestValidationException { }
    }
}
