using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Services.Mailer;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using GraphQL.Conventions;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.DbModel.Entities;
using System;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using System.Linq;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Requests.Commands.Mutations.Transactions
{
    public class RefundTransaction : IRequestHandler<RefundTransaction.Input, RefundTransaction.Payload>
    {
        private readonly ILogger<RefundTransaction> logger;
        private readonly AppDbContext db;
        private readonly IMailer mailer;
        private readonly IClock clock;
        private readonly IHttpContextAccessor httpContextAccessor;
        
        private AppUser currentUser;
        private DateTime today;
        private List<TransactionLog> transactionLogs;
        private TransactionLog baseTransactionLog;

        public RefundTransaction(ILogger<RefundTransaction> logger, AppDbContext db, IMailer mailer, IClock clock, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.db = db;
            this.mailer = mailer;
            this.clock = clock;
            this.httpContextAccessor = httpContextAccessor;
            transactionLogs = new List<TransactionLog>();
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            today = clock
                .GetCurrentInstant()
                .InUtc()
                .ToDateTimeUtc();

            var initialTransactionId = request.InitialTransactionId.LongIdentifierForType<PaymentTransaction>();
            var initialTransaction = await db.Transactions.OfType<PaymentTransaction>()
                .Include(x => x.Card).ThenInclude(x => x.Funds).ThenInclude(x => x.ProductGroup)
                .Include(x => x.Card).ThenInclude(x => x.Project)
                .Include(x => x.Beneficiary)
                .Include(x => x.Market)
                .Include(x => x.TransactionByProductGroups)
                .Include(x => x.RefundTransactions)
                .Include(x => x.Organization)
                .Include(x => x.Transactions)
                .FirstOrDefaultAsync(x => x.Id == initialTransactionId, cancellationToken);

            if (initialTransaction == null) throw new InitialTransactionNotFoundException();

            var refundTransaction = new DbModel.Entities.Transactions.RefundTransaction()
            {
                Beneficiary = initialTransaction.Beneficiary,
                Card = initialTransaction.Card,
                CreatedAtUtc = today,
                InitialTransaction = initialTransaction,
                Organization = initialTransaction.Organization,
                TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(),
                Amount = request.Transactions.Sum(x => x.Amount),
                RefundByProductGroups = new List<RefundTransactionProductGroup>()
            };

            var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            currentUser = db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId);

            var beneficiary = initialTransaction.Beneficiary;
            var cardName = beneficiary != null ? $"{beneficiary.Firstname} {beneficiary.Lastname}" : initialTransaction.CardId.ToString();
            var market = initialTransaction.Market;
            var card = initialTransaction.Card;

            baseTransactionLog = new TransactionLog()
            {
                Discriminator = TransactionLogDiscriminator.RefundPaymentTransactionLog,
                TransactionUniqueId = refundTransaction.TransactionUniqueId,
                CreatedAtUtc = today,
                TotalAmount = 0,
                MarketId = market.Id,
                MarketName = market.Name,
                CardProgramCardId = card.ProgramCardId,
                CardNumber = card.CardNumber,
                BeneficiaryId = card.Beneficiary?.Id,
                BeneficiaryID1 = card.Beneficiary?.ID1,
                BeneficiaryID2 = card.Beneficiary?.ID2,
                BeneficiaryFirstname = card.Beneficiary?.Firstname,
                BeneficiaryLastname = card.Beneficiary?.Lastname,
                BeneficiaryEmail = card.Beneficiary?.Email,
                BeneficiaryPhone = card.Beneficiary?.Phone,
                BeneficiaryIsOffPlatform = card.Beneficiary is OffPlatformBeneficiary,
                BeneficiaryTypeId = card.Beneficiary?.BeneficiaryTypeId,
                OrganizationId = card.Beneficiary?.OrganizationId,
                OrganizationName = card.Beneficiary?.Organization?.Name,
                ProjectId = card.ProjectId,
                TransactionInitiatorId = currentUser?.Id,
                TransactionInitiatorFirstname = currentUser?.Profile.FirstName,
                TransactionInitiatorLastname = currentUser?.Profile.LastName,
                TransactionInitiatorEmail = currentUser?.Email,
                TransactionLogProductGroups = new List<TransactionLogProductGroup>()
            };
            transactionLogs.Add(baseTransactionLog);

            foreach (var refund in request.Transactions)
            {
                var productGroupId = refund.ProductGroupId.LongIdentifierForType<ProductGroup>();
                var paymentTransactionProductGroup = initialTransaction.TransactionByProductGroups.Where(x => x.ProductGroupId == productGroupId).FirstOrDefault();
                var fund = initialTransaction.Card.Funds.Where(x => x.ProductGroupId == productGroupId).FirstOrDefault();
                var addingFundTransaction = initialTransaction.Transactions.Where(x => x.ProductGroupId == productGroupId).FirstOrDefault();

                if (paymentTransactionProductGroup == null) throw new ProductGroupNotFoundException();

                if (paymentTransactionProductGroup.Amount - paymentTransactionProductGroup.RefundAmount < refund.Amount) throw new TooMuchRefundException();

                refundTransaction.RefundByProductGroups.Add(new RefundTransactionProductGroup()
                {
                    Amount = refund.Amount,
                    ProductGroupId = paymentTransactionProductGroup.ProductGroupId,
                    RefundTransaction = refundTransaction,
                    PaymentTransactionProductGroup = paymentTransactionProductGroup
                });

                paymentTransactionProductGroup.RefundAmount += refund.Amount;
                if (addingFundTransaction.Status == FundTransactionStatus.Actived)
                {
                    addingFundTransaction.AvailableFund += refund.Amount;
                    fund.Amount += refund.Amount;
                }

                baseTransactionLog.TotalAmount += refund.Amount;
                baseTransactionLog.TransactionLogProductGroups.Add(new TransactionLogProductGroup()
                {
                    Amount = refund.Amount,
                    ProductGroupId = paymentTransactionProductGroup.ProductGroupId,
                    TransactionLog = baseTransactionLog
                });
            }

            initialTransaction.RefundTransactions.Add(refundTransaction);
            db.TransactionLogs.AddRange(transactionLogs);

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"Transaction refund between {cardName} with ({market.Name}) for an amount of {request.Transactions.Sum(x => x.Amount)} for product group(s) {request.Transactions.Select(x => x.ProductGroupId)}");

            if (beneficiary != null && !string.IsNullOrEmpty(beneficiary.Email))
            {
                try
                {
                    await mailer.Send(new TransactionRefundBeneficiaryReceiptEmail(beneficiary.Email.Trim(), market.Name,
                        card.Project.Name, card.Project.Url, request.Transactions.Sum(x => x.Amount), card.TotalFund(),
                        card.Funds.Select(x => new RefundProductGroupAvailableFund()
                        {
                            Fund = x.Amount,
                            Name = x.ProductGroup.Name == ProductGroupType.LOYALTY
                                ? "Carte-cadeau/Gift-card"
                                : x.ProductGroup.Name
                        })));
                }
                catch (Exception e)
                {
                    logger.LogError($"Could not send refund confirmation email to ({cardName}) for transaction with ({market.Name}). Error message: {e.Message}");
                }
            }

            return new Payload()
            {
                Transaction = new RefundTransactionGraphType(refundTransaction)
            };
        }

        [MutationInput]
        public class Input : IRequest<Payload>, IHaveInitialTransactionId
        {
            public Id InitialTransactionId { get; set; }
            public List<RefundTransactionsInput> Transactions { get; set; }
        }

        [InputType]
        public class RefundTransactionsInput
        {
            public decimal Amount { get; set; }
            public Id ProductGroupId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public RefundTransactionGraphType Transaction { get; set; }
        }

        public class WrongPasswordException : RequestValidationException { }
        public class InitialTransactionNotFoundException : RequestValidationException { }
        public class ProductGroupNotFoundException : RequestValidationException { }
        public class TooMuchRefundException : RequestValidationException { }
    }
}
