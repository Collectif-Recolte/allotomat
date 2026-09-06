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
using Microsoft.AspNetCore.Identity;
using Sig.App.Backend.Gql.Bases;

namespace Sig.App.Backend.Requests.Commands.Mutations.Transactions
{
    public class RefundTransaction : IRequestHandler<RefundTransaction.Input, RefundTransaction.Payload>
    {
        private readonly ILogger<RefundTransaction> logger;
        private readonly AppDbContext db;
        private readonly IMailer mailer;
        private readonly IClock clock;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly UserManager<AppUser> userManager;

        private AppUser currentUser;
        private DateTime today;
        private List<TransactionLog> transactionLogs;
        private TransactionLog baseTransactionLog;

        public RefundTransaction(ILogger<RefundTransaction> logger, AppDbContext db, IMailer mailer, IClock clock, IHttpContextAccessor httpContextAccessor, UserManager<AppUser> userManager)
        {
            this.logger = logger;
            this.db = db;
            this.mailer = mailer;
            this.clock = clock;
            this.httpContextAccessor = httpContextAccessor;
            this.userManager = userManager;
            transactionLogs = new List<TransactionLog>();
        }

        /// <summary>
        /// Tentatives avant d'abandonner, même raison que <c>CreateTransaction</c> : une vraie course
        /// se règle en un rejeu, trois échecs de suite cachent autre chose.
        /// </summary>
        public const int MaxPlanningAttempts = 3;

        /// <summary>
        /// CRCL-2669 - Le plafond de remboursement (<c>Amount - RefundAmount &lt; demandé</c>) est lu
        /// au début, et l'écriture qui le consomme (<c>RefundAmount += demandé</c>) part à la fin.
        /// Deux remboursements du même paiement - deux employés, ou un double clic sur le bouton -
        /// passaient donc tous les deux le contrôle, et le second écrasait le compteur du premier :
        /// le paiement restait remboursable alors qu'il avait déjà tout rendu.
        ///
        /// Le compteur est maintenant un jeton (voir <c>AppDbContext</c>), donc le perdant lève au
        /// lieu d'écraser, et le remboursement est REPLANIFIÉ sur des données fraîches : le plafond
        /// voit alors le premier remboursement et tranche pour de vrai - il passe s'il reste de la
        /// place, il lève <c>TooMuchRefundException</c> sinon.
        ///
        /// Sans ça, le joint aggravait le cas au lieu de le corriger : les deux crédits de carte sont
        /// rebasés l'un sur l'autre au lieu de s'écraser, donc l'argent était bel et bien rendu deux
        /// fois, là où l'ancien écrasement en perdait un et masquait la duplication.
        /// </summary>
        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await HandleAttempt(request, cancellationToken);
                }
                catch (DbUpdateConcurrencyException exception) when (attempt < MaxPlanningAttempts)
                {
                    // Vider le suivi force la relecture : une requête suivie rendrait le paiement
                    // déjà en mémoire, avec le RefundAmount périmé qui a causé le conflit.
                    logger.LogWarning($"[Mutation] RefundTransaction - Plafond de remboursement modifié par une écriture concurrente, replanification sur des données fraîches (tentative {attempt} de {MaxPlanningAttempts}) : {exception.Message}");
                    db.ChangeTracker.Clear();
                    transactionLogs = new List<TransactionLog>();
                }
            }
        }

        private async Task<Payload> HandleAttempt(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] RefundTransaction({request.InitialTransactionId}, {request.Transactions})");
            today = clock
                .GetCurrentInstant()
                .InUtc()
                .ToDateTimeUtc();

            var initialTransactionId = request.InitialTransactionId.LongIdentifierForType<PaymentTransaction>();
            var initialTransaction = await db.Transactions.OfType<PaymentTransaction>()
                .Include(x => x.Card).ThenInclude(x => x.Funds).ThenInclude(x => x.ProductGroup)
                .Include(x => x.Card).ThenInclude(x => x.Project)
                .AsSplitQuery()
                .Include(x => x.Beneficiary)
                .Include(x => x.Market)
                .Include(x => x.TransactionByProductGroups)
                .Include(x => x.RefundTransactions)
                .Include(x => x.Organization)
                .Include(x => x.Transactions)
                .Include(x => x.PaymentTransactionAddingFundTransactions).ThenInclude(x => x.AddingFundTransaction)
                .Include(x => x.CashRegister).ThenInclude(x => x.MarketGroups).ThenInclude(x => x.MarketGroup)
                .FirstOrDefaultAsync(x => x.Id == initialTransactionId, cancellationToken);

            if (initialTransaction == null)
            {
                logger.LogWarning("[Mutation] RefundTransaction - InitialTransactionNotFoundException");
                throw new InitialTransactionNotFoundException();
            }

            if (initialTransaction.Market.IsDisabled)
            {
                throw new MarketDisabledException();
            }

            var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            currentUser = db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId);

            var isValid = await userManager.CheckPasswordAsync(currentUser, request.Password);
            if (!isValid)
            {
                logger.LogWarning("[Mutation] RefundTransaction - WrongPasswordException");
                throw new WrongPasswordException();
            }

            var refundTransaction = new DbModel.Entities.Transactions.RefundTransaction()
            {
                Beneficiary = initialTransaction.Beneficiary,
                Card = initialTransaction.Card,
                CreatedAtUtc = today,
                InitialTransaction = initialTransaction,
                Organization = initialTransaction.Organization,
                TransactionUniqueId = TransactionHelper.CreateTransactionUniqueId(),
                Amount = request.Transactions.Sum(x => x.Amount),
                RefundByProductGroups = new List<RefundTransactionProductGroup>(),
                CashRegister = initialTransaction.CashRegister
            };

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
                ProjectName = card.Project.Name,
                TransactionInitiatorId = currentUser?.Id,
                TransactionInitiatorFirstname = currentUser?.Profile.FirstName,
                TransactionInitiatorLastname = currentUser?.Profile.LastName,
                TransactionInitiatorEmail = currentUser?.Email,
                TransactionLogProductGroups = new List<TransactionLogProductGroup>(),
                InitiatedByProject = currentUser?.Type == UserType.ProjectManager,
                InitiatedByOrganization = currentUser?.Type == UserType.OrganizationManager,
                CashRegisterId = initialTransaction.CashRegisterId,
                CashRegisterName = initialTransaction.CashRegister?.Name,
                MarketGroupId = initialTransaction.CashRegister?.MarketGroups?.FirstOrDefault(x => x.MarketGroup.ProjectId == card.ProjectId)?.MarketGroupId,
                MarketGroupName = initialTransaction.CashRegister?.MarketGroups?.FirstOrDefault(x => x.MarketGroup.ProjectId == card.ProjectId)?.MarketGroup?.Name
            };
            transactionLogs.Add(baseTransactionLog);

            foreach (var refund in request.Transactions)
            {
                var productGroupId = refund.ProductGroupId.LongIdentifierForType<ProductGroup>();
                var paymentTransactionProductGroup = initialTransaction.TransactionByProductGroups.Where(x => x.ProductGroupId == productGroupId).FirstOrDefault();

                if (paymentTransactionProductGroup == null)
                {
                    logger.LogWarning("[Mutation] RefundTransaction - ProductGroupNotFoundException");
                    throw new ProductGroupNotFoundException();
                }

                // CRCL-2669 - Le plafond se contrôle sur la valeur EN BASE, et non sur celle qu'a lue
                // le handler : entre les deux, un autre remboursement du même paiement a pu la
                // consommer - deux employés, ou un double clic sur le bouton. Relire ici refuse tout
                // de suite, au lieu de tout préparer pour se faire arrêter par le jeton au moment
                // d'écrire. Le jeton reste le filet pour la fenêtre qui suit cette relecture.
                var persistedRefundAmount = await db.PaymentTransactionProductGroups.AsNoTracking()
                    .Where(x => x.Id == paymentTransactionProductGroup.Id)
                    .Select(x => x.RefundAmount)
                    .SingleAsync(cancellationToken);

                if (paymentTransactionProductGroup.Amount - persistedRefundAmount < refund.Amount)
                {
                    logger.LogWarning("[Mutation] RefundTransaction - TooMuchRefundException");
                    throw new TooMuchRefundException();
                }

                var fund = initialTransaction.Card.Funds.Where(x => x.ProductGroupId == productGroupId).FirstOrDefault();

                var refundTransactionProductGroup = new RefundTransactionProductGroup()
                {
                    Amount = refund.Amount,
                    ProductGroupId = paymentTransactionProductGroup.ProductGroupId,
                    RefundTransaction = refundTransaction,
                    PaymentTransactionProductGroup = paymentTransactionProductGroup
                };
                refundTransaction.RefundByProductGroups.Add(refundTransactionProductGroup);

                paymentTransactionProductGroup.RefundAmount += refund.Amount;

                if (initialTransaction.PaymentTransactionAddingFundTransactions.Any())
                {
                    await AssignSubscriptionFromFundSources(
                        baseTransactionLog,
                        initialTransaction.PaymentTransactionAddingFundTransactions.Select(x => x.AddingFundTransaction),
                        cancellationToken);

                    var paymentTransactionAddingFundTransactions = initialTransaction.PaymentTransactionAddingFundTransactions.Where(x => x.AddingFundTransaction.ProductGroupId == productGroupId).ToList();
                    var amountToRefund = refund.Amount;
                    
                    foreach (var paymentTransactionAddingFundTransaction in paymentTransactionAddingFundTransactions)
                    {
                        if (paymentTransactionAddingFundTransaction.AddingFundTransaction.Status == FundTransactionStatus.Actived)
                        {
                            var amount = Math.Min(amountToRefund, paymentTransactionAddingFundTransaction.Amount - paymentTransactionAddingFundTransaction.RefundAmount);
                            paymentTransactionAddingFundTransaction.AddingFundTransaction.AvailableFund += amount;
                            fund.Amount += amount;
                            refundTransactionProductGroup.AmountRefunded += amount;
                            paymentTransactionAddingFundTransaction.RefundAmount += amount;
                            amountToRefund -= amount;
                        }
                    }

                    if (amountToRefund > 0)
                    {
                        logger.LogWarning("[Mutation] RefundTransaction - TooMuchRefundException");
                        throw new TooMuchRefundException();
                    }
                }
                else
                {
                    await AssignSubscriptionFromFundSources(
                        baseTransactionLog,
                        initialTransaction.Transactions,
                        cancellationToken);

                    var addingFundTransaction = initialTransaction.Transactions.Where(x => x.ProductGroupId == productGroupId).FirstOrDefault();
                    if (addingFundTransaction.Status == FundTransactionStatus.Actived)
                    {
                        addingFundTransaction.AvailableFund += refund.Amount;
                        fund.Amount += refund.Amount;
                        refundTransactionProductGroup.AmountRefunded += refund.Amount;
                    }
                }

                baseTransactionLog.TotalAmount += refund.Amount;
                baseTransactionLog.TransactionLogProductGroups.Add(new TransactionLogProductGroup()
                {
                    Amount = refund.Amount,
                    ProductGroupId = paymentTransactionProductGroup.ProductGroupId,
                    ProductGroupName = paymentTransactionProductGroup.ProductGroup.Name,
                    TransactionLog = baseTransactionLog
                });
            }

            initialTransaction.RefundTransactions.Add(refundTransaction);
            db.TransactionLogs.AddRange(transactionLogs);

            await db.SaveChangesWithFundRetryAsync(cancellationToken);

            logger.LogInformation($"[Mutation] RefundTransaction - Transaction refund between {cardName} with ({market.Name}) for an amount of {request.Transactions.Sum(x => x.Amount)} for product group(s) {request.Transactions.Select(x => x.ProductGroupId)}");

            if (beneficiary != null && !string.IsNullOrEmpty(beneficiary.Email) && !beneficiary.IsUnsubscribeToReceipt)
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
                        }), beneficiary.GetIdentifier().ToString()));
                }
                catch (Exception e)
                {
                    logger.LogError($"[Mutation] RefundTransaction - Could not send refund confirmation email to ({cardName}) for transaction with ({market.Name}). Error message: {e.Message}");
                }
            }

            return new Payload()
            {
                Transaction = new RefundTransactionGraphType(refundTransaction)
            };
        }

        private async Task AssignSubscriptionFromFundSources(
            TransactionLog transactionLog,
            IEnumerable<AddingFundTransaction> fundSources,
            CancellationToken cancellationToken)
        {
            if (transactionLog.SubscriptionId.HasValue)
            {
                return;
            }

            var addingFundTransactions = fundSources
                .Where(x => x is ManuallyAddingFundTransaction || x is SubscriptionAddingFundTransaction)
                .ToList();

            if (!addingFundTransactions.Any())
            {
                return;
            }

            var groupedBySubscription = await TransactionHelper.GroupAddingFundTransactionsBySubscriptionId(
                db, addingFundTransactions, cancellationToken);
            var subscriptionGroup = groupedBySubscription.FirstOrDefault(x => x.Key != -1);
            if (subscriptionGroup == null)
            {
                return;
            }

            var subscription = await db.Subscriptions.FirstOrDefaultAsync(x => x.Id == subscriptionGroup.Key, cancellationToken);
            if (subscription == null)
            {
                return;
            }

            transactionLog.SubscriptionId = subscription.Id;
            transactionLog.SubscriptionName = subscription.Name;
        }

        [MutationInput]
        public class Input : HaveInitialTransactionId, IRequest<Payload>
        {
            public string Password { get; set; }
            public List<RefundTransactionsInput> Transactions { get; set; }
        }

        [InputType]
        public class RefundTransactionsInput
        {
            public decimal Amount { get; set; }
            public Id ProductGroupId { get; set; }

            public override string ToString()
            {
                return $"{Amount}, {ProductGroupId}";
            }
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
        public class MarketDisabledException : RequestValidationException { }
    }
}
