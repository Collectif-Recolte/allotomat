using System;
using System.Collections.Generic;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.Gql.Bases;

namespace Sig.App.Backend.Requests.Commands.Mutations.Transactions
{
    public class CreateManuallyAddingFundTransaction : IRequestHandler<CreateManuallyAddingFundTransaction.Input, CreateManuallyAddingFundTransaction.Payload>
    {
        private readonly ILogger<CreateManuallyAddingFundTransaction> logger;
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly IHttpContextAccessor httpContextAccessor;

        public CreateManuallyAddingFundTransaction(ILogger<CreateManuallyAddingFundTransaction> logger, AppDbContext db, IClock clock, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.db = db;
            this.clock = clock;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] CreateManuallyAddingFundTransaction({request.SubscriptionId}, {request.Amount}, {request.ProductGroupId})");
            var today = clock
                .GetCurrentInstant()
                .InUtc()
                .ToDateTimeUtc();
            var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            var currentUser = db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId);

            long beneficiaryId;
            var isOffPlatformBeneficiary = false;
            
            if (request.BeneficiaryId.IsIdentifierForType(typeof(Beneficiary)))
            {
                beneficiaryId = request.BeneficiaryId.LongIdentifierForType<Beneficiary>();
            }
            else
            {
                isOffPlatformBeneficiary = true;
                beneficiaryId = request.BeneficiaryId.LongIdentifierForType<OffPlatformBeneficiary>();
            }

            var beneficiary = await db.Beneficiaries
                .Include(x => x.Card).ThenInclude(x => x.Transactions)
                .Include(x => x.Card).ThenInclude(x => x.Funds)
                .Include(x => x.Organization).ThenInclude(x => x.Project)
                .Include(x => x.Subscriptions)
                .FirstOrDefaultAsync(x => x.Id == beneficiaryId, cancellationToken);

            if (beneficiary == null) throw new BeneficiaryNotFoundException();

            if (beneficiary.Card == null) throw new BeneficiaryDontHaveCardException();

            var productGroupId = request.ProductGroupId.LongIdentifierForType<ProductGroup>();
            var productGroup = await db.ProductGroups.FirstOrDefaultAsync(x => x.Id == productGroupId, cancellationToken);

            if (productGroup == null) throw new ProductGroupNotFoundException();

            var card = beneficiary.Card;

            var fund = card.Funds.FirstOrDefault(x => x.ProductGroupId == productGroupId);
            if (fund == null)
            {
                fund = new Fund()
                {
                    Card = card,
                    ProductGroup = productGroup
                };

                db.Funds.Add(fund);
            }

            if(fund.Amount + request.Amount < 0) throw new AvailableFundCantBeLessThanZero();
            fund.Amount += request.Amount;

            AddingFundTransaction transaction;
            var transactionUniqueId = TransactionHelper.CreateTransactionUniqueId();
            if (!isOffPlatformBeneficiary)
            {
                var subscriptionId = request.SubscriptionId.Value.LongIdentifierForType<Subscription>();
                var subscription = await db.Subscriptions.Include(x => x.Types).ThenInclude(x => x.ProductGroup).FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

                if (subscription == null) throw new SubscriptionNotFoundException();

                if (!beneficiary.Subscriptions.Any(x => x.SubscriptionId == subscriptionId)) throw new BeneficiaryDontHaveThisSubscriptionException();

                if (subscription.IsFundsAccumulable && subscription.FundsExpirationDate < today) throw new SubscriptionExpiredException();

                var budgetAllowance = await db.BudgetAllowances.FirstOrDefaultAsync(x => x.OrganizationId == beneficiary.OrganizationId && x.SubscriptionId == subscriptionId, cancellationToken);

                if (budgetAllowance == null) throw new SubscriptionDontHaveBudgetAllowance();

                if (budgetAllowance.AvailableFund < request.Amount) throw new SubscriptionDontHaveEnoughtAvailableAmount();

                if (!subscription.Types.Select(x => x.ProductGroup).Any(x => x.Id == productGroupId)) throw new ProductGroupNotFoundInSubscriptionException();

                transaction = new ManuallyAddingFundTransaction()
                {
                    TransactionUniqueId = transactionUniqueId,
                    Card = card,
                    Beneficiary = beneficiary,
                    OrganizationId = beneficiary.OrganizationId,
                    Amount = request.Amount,
                    AvailableFund = request.Amount,
                    CreatedAtUtc = today,
                    ExpirationDate = subscription.GetExpirationDate(clock, subscription.MonthlyPaymentMoment),
                    Subscription = subscription,
                    ProductGroup = productGroup
                };
                beneficiary.Card.Transactions.Add(transaction);
                budgetAllowance.AvailableFund -= request.Amount;
            }
            else
            {
                transaction = new OffPlatformAddingFundTransaction()
                {
                    TransactionUniqueId = transactionUniqueId,
                    Card = beneficiary.Card,
                    Beneficiary = beneficiary,
                    OrganizationId = beneficiary.OrganizationId,
                    Amount = request.Amount,
                    AvailableFund = request.Amount,
                    CreatedAtUtc = today,
                    ExpirationDate = SubscriptionHelper.GetNextPaymentDateTime(clock, (beneficiary as OffPlatformBeneficiary).MonthlyPaymentMoment.HasValue ? (beneficiary as OffPlatformBeneficiary).MonthlyPaymentMoment.Value : SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth),
                    ProductGroup = productGroup
                };
                beneficiary.Card.Transactions.Add(transaction);
            }

            var transactionLogProductGroups = new List<TransactionLogProductGroup>()
            {
                new()
                {
                    Amount = transaction.Amount,
                    ProductGroupId = productGroupId,
                    ProductGroupName = productGroup.Name
                }
            };
            
            var currentSubscription = transaction is ManuallyAddingFundTransaction maft ? maft.Subscription : null;
            db.TransactionLogs.Add(new TransactionLog()
            {
                Discriminator = TransactionLogDiscriminator.ManuallyAddingFundTransactionLog,
                TransactionUniqueId = transactionUniqueId,
                CreatedAtUtc = today,
                TotalAmount = transaction.Amount,
                CardProgramCardId = card.ProgramCardId,
                CardNumber = card.CardNumber,
                BeneficiaryId = card.Beneficiary.Id,
                BeneficiaryID1 = card.Beneficiary.ID1,
                BeneficiaryID2 = card.Beneficiary.ID2,
                BeneficiaryFirstname = card.Beneficiary.Firstname,
                BeneficiaryLastname = card.Beneficiary.Lastname,
                BeneficiaryEmail = card.Beneficiary.Email,
                BeneficiaryPhone = card.Beneficiary.Phone,
                BeneficiaryIsOffPlatform = card.Beneficiary is OffPlatformBeneficiary,
                BeneficiaryTypeId = card.Beneficiary.BeneficiaryTypeId,
                OrganizationId = card.Beneficiary.OrganizationId,
                OrganizationName = card.Beneficiary.Organization.Name,
                SubscriptionId = currentSubscription?.Id,
                SubscriptionName = currentSubscription?.Name,
                ProjectId = card.Beneficiary.Organization.ProjectId,
                ProjectName = card.Beneficiary.Organization.Project.Name,
                TransactionInitiatorId = currentUserId,
                TransactionInitiatorFirstname = currentUser?.Profile.FirstName,
                TransactionInitiatorLastname = currentUser?.Profile.LastName,
                TransactionInitiatorEmail = currentUser?.Email,
                TransactionLogProductGroups = transactionLogProductGroups
            });

            logger.LogInformation($"Adding manual fund {request.Amount} to ({beneficiary.Id}) card for product group {productGroupId}");

            await db.SaveChangesAsync(cancellationToken);

            if (!isOffPlatformBeneficiary)
            {
                return new Payload()
                {
                    Transaction = new ManuallyAddingFundTransactionGraphType(transaction as ManuallyAddingFundTransaction)
                };
            }
            
            return new Payload()
            {
                Transaction = new OffPlatformAddingFundTransactionGraphType(transaction as OffPlatformAddingFundTransaction)
            };
        }

        [MutationInput]
        public class Input : HaveBeneficiaryId, IRequest<Payload>
        {
            public Maybe<Id> SubscriptionId { get; set; }
            public decimal Amount { get; set; }
            public Id ProductGroupId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public IAddingFundTransactionGraphType Transaction { get; set; }
        }

        public class AvailableFundCantBeLessThanZero : RequestValidationException { }
        public class BeneficiaryNotFoundException : RequestValidationException { }
        public class BeneficiaryDontHaveCardException : RequestValidationException { }
        public class BeneficiaryDontHaveThisSubscriptionException : RequestValidationException { }
        public class SubscriptionNotFoundException : RequestValidationException { }
        public class SubscriptionExpiredException : RequestValidationException { }
        public class SubscriptionDontHaveBudgetAllowance : RequestValidationException { }
        public class SubscriptionDontHaveEnoughtAvailableAmount : RequestValidationException { }
        public class ProductGroupNotFoundException : RequestValidationException { }
        public class ProductGroupNotFoundInSubscriptionException : RequestValidationException { }
    }
}
