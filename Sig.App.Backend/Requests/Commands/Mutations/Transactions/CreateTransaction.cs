using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Requests.Queries.Cards;
using Sig.App.Backend.Services.Mailer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Gql.Bases;

namespace Sig.App.Backend.Requests.Commands.Mutations.Transactions
{
    public class CreateTransaction : IRequestHandler<CreateTransaction.Input, CreateTransaction.Payload>
    {
        private readonly ILogger<CreateTransaction> logger;
        private readonly AppDbContext db;
        private readonly IMediator mediator;
        private readonly IMailer mailer;
        private readonly IClock clock;
        private readonly IHttpContextAccessor httpContextAccessor;
        private AppUser currentUser;
        private DateTime today;
        private List<TransactionLog> transactionLogs;

        public CreateTransaction(ILogger<CreateTransaction> logger, AppDbContext db, IMediator mediator, IMailer mailer, IClock clock, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.db = db;
            this.mediator = mediator;
            this.mailer = mailer;
            this.clock = clock;
            this.httpContextAccessor = httpContextAccessor;
            transactionLogs = new List<TransactionLog>();
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] CreateTransaction({request.CardId}, {request.CardNumber}, {request.Transactions})");
            long cardId = -1;
            if (request.CardId.HasValue)
            {
                cardId = request.CardId.Value.LongIdentifierForType<Card>();
            }
            else if (!string.IsNullOrEmpty(request.CardNumber))
            {
                cardId = await db.Cards.Where(x => x.CardNumber == request.CardNumber).Select(x => x.Id).FirstOrDefaultAsync(cancellationToken);
            }

            var card = await db.Cards.Include(x => x.Project).Include(x => x.Beneficiary)
                .ThenInclude(x => x.Organization).Include(x => x.Transactions).Include(x => x.Funds)
                .ThenInclude(x => x.ProductGroup).FirstOrDefaultAsync(x => x.Id == cardId, cancellationToken);

            if (card == null)
            {
                logger.LogWarning("[Mutation] CreateTransaction - CardNotFoundException");
                throw new CardNotFoundException();
            }

            var martketId = request.MarketId.LongIdentifierForType<Market>();
            var market = await db.Markets.FirstOrDefaultAsync(x => x.Id == martketId, cancellationToken);

            if (market == null)
            {
                logger.LogWarning("[Mutation] CreateTransaction - MarketNotFoundException");
                throw new MarketNotFoundException();
            }

            var cardCanBeUsedInMarket = await mediator.Send(new VerifyCardCanBeUsedInMarket.Input
            {
                MarketId = request.MarketId,
                CardId = card.GetIdentifier()
            }, cancellationToken);

            if (!cardCanBeUsedInMarket)
            {
                logger.LogWarning("[Mutation] CreateTransaction - CardCantBeUsedInMarketException");
                throw new CardCantBeUsedInMarketException();
            }
            
            today = clock
                .GetCurrentInstant()
                .InUtc()
                .ToDateTimeUtc();
            var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            currentUser = db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId);

            var affectedAddingFundTransactions = new List<AddingFundTransaction>();

            var beneficiary = card.Beneficiary;
            var organizationId = beneficiary?.OrganizationId;
            var loyaltyFundTransactions = card.Transactions
                .OfType<LoyaltyAddingFundTransaction>()
                .Where(x => x.Status == FundTransactionStatus.Actived && x.AvailableFund > 0)
                .ToList();

            var transactionByProductGroups = new List<PaymentTransactionProductGroup>();
            decimal loyaltyFundToRemove = request.Transactions.Sum(x => x.Amount);

            var transactionUniqueId = TransactionHelper.CreateTransactionUniqueId();
            var transaction = new PaymentTransaction()
            {
                TransactionUniqueId = transactionUniqueId,
                Amount = request.Transactions.Sum(x => x.Amount),
                Card = card,
                Beneficiary = beneficiary,
                OrganizationId = organizationId,
                Market = market,
                CreatedAtUtc = today,
                InitiatedByProject = currentUser?.Type == UserType.ProjectManager
            };

            foreach (var transactionInput in request.Transactions)
            {
                var productGroupId = transactionInput.ProductGroupId.LongIdentifierForType<ProductGroup>();
                var productGroup = db.ProductGroups.FirstOrDefault(x => x.Id == productGroupId);

                var amount = decimal.Round(transactionInput.Amount, 2);

                if (productGroup != null && productGroup.Name != ProductGroupType.LOYALTY)
                {
                    var fund = card.Funds.FirstOrDefault(x => x.ProductGroupId == productGroupId);

                    if (fund == null)
                    {
                        logger.LogWarning("[Mutation] CreateTransaction - CardDontHaveFundType");
                        throw new CardDontHaveFundType();
                    }

                    if (fund.Amount + card.LoyaltyFund() < transactionInput.Amount)
                    {
                        logger.LogWarning("[Mutation] CreateTransaction - NotEnoughtFundException");
                        throw new NotEnoughtFundException();
                    }

                    var addingFundTransactions = card.Transactions
                        .Where(x => x is SubscriptionAddingFundTransaction or ManuallyAddingFundTransaction)
                        .OfType<AddingFundTransaction>()
                        .Where(x => x.Status == FundTransactionStatus.Actived && x.AvailableFund > 0 && x.ProductGroupId == productGroupId)
                        .ToList();

                    var fundToRemove = Math.Min(fund.Amount, amount);
                    
                    if (addingFundTransactions.Any())
                    {
                        var tempAmount = amount;

                        var addingFundTransactionsBySubscriptionId =
                            await TransactionHelper.GroupAddingFundTransactionsBySubscriptionId(db,
                                addingFundTransactions,
                                cancellationToken);
                        var subscriptions = await db.Subscriptions
                            .Where(x => addingFundTransactionsBySubscriptionId.Select(y => y.Key).Contains(x.Id))
                            .ToListAsync(cancellationToken);

                        foreach (var addingFundTransaction in addingFundTransactions)
                        {
                            var subscriptionId = addingFundTransactionsBySubscriptionId.First(x =>
                                x.Any(y => y.Id == addingFundTransaction.Id)).Key;
                            var subscription = subscriptions.First(x => x.Id == subscriptionId);
                            if (tempAmount > addingFundTransaction.AvailableFund)
                            {
                                AddAmountToTransactionLog(transaction, card, market, subscription, productGroup,
                                    addingFundTransaction.AvailableFund);
                                tempAmount -= addingFundTransaction.AvailableFund;
                                loyaltyFundToRemove -= addingFundTransaction.AvailableFund;
                                addingFundTransaction.AvailableFund = 0;
                            }
                            else
                            {
                                AddAmountToTransactionLog(transaction, card, market, subscription, productGroup,
                                    tempAmount);
                                addingFundTransaction.AvailableFund -= tempAmount;
                                loyaltyFundToRemove -= tempAmount;
                                tempAmount = 0;
                            }

                            affectedAddingFundTransactions.Add(addingFundTransaction);

                            if (tempAmount == 0)
                            {
                                break;
                            }
                        }
                    }
                    else if (card.Project.AdministrationSubscriptionsOffPlatform)
                    {
                        // Beneficiary is off platform
                        AddAmountToTransactionLog(transaction, card, market, null, productGroup, fundToRemove);
                        loyaltyFundToRemove -= fundToRemove;
                    }

                    transactionByProductGroups.Add(new PaymentTransactionProductGroup()
                    {
                        Amount = fundToRemove,
                        ProductGroup = productGroup,
                        PaymentTransaction = transaction
                    });

                    fund.Amount -= fundToRemove;
                }
            }

            if (loyaltyFundToRemove > 0)
            {
                var loyaltyFund = card.Funds.FirstOrDefault(x => x.ProductGroup.Name == ProductGroupType.LOYALTY);
                if (loyaltyFund != null)
                {
                    foreach (var loyaltyFundTransaction in loyaltyFundTransactions)
                    {
                        decimal fundToRemove;
                        if (loyaltyFundToRemove > loyaltyFundTransaction.AvailableFund)
                        {
                            fundToRemove = loyaltyFundTransaction.AvailableFund;
                            loyaltyFundToRemove -= loyaltyFundTransaction.AvailableFund;
                            loyaltyFund.Amount -= loyaltyFundTransaction.AvailableFund;
                            loyaltyFundTransaction.AvailableFund = 0;
                        }
                        else
                        {
                            fundToRemove = loyaltyFundToRemove;
                            loyaltyFundTransaction.AvailableFund -= loyaltyFundToRemove;
                            loyaltyFund.Amount -= loyaltyFundToRemove;
                            loyaltyFundToRemove = 0;
                        }

                        affectedAddingFundTransactions.Add(loyaltyFundTransaction);

                        transactionByProductGroups.Add(new PaymentTransactionProductGroup()
                        {
                            Amount = fundToRemove,
                            ProductGroup = loyaltyFund.ProductGroup,
                            PaymentTransaction = transaction
                        });

                        AddAmountToTransactionLog(transaction, card, market, null, loyaltyFund.ProductGroup, fundToRemove);

                        if (loyaltyFundToRemove == 0)
                        {
                            break;
                        }
                    }
                }
            }

            if (loyaltyFundToRemove > 0)
            {
                logger.LogWarning("[Mutation] CreateTransaction - NotEnoughtFundException");
                throw new NotEnoughtFundException();
            }

            transaction.Transactions = affectedAddingFundTransactions;
            transaction.TransactionByProductGroups = transactionByProductGroups;
            card.Transactions.Add(transaction);
            db.TransactionLogs.AddRange(transactionLogs);
            await db.SaveChangesAsync(cancellationToken);

            var cardName = beneficiary != null ? $"{card.Beneficiary.Firstname} {card.Beneficiary.Lastname}" : card.Id.ToString();
            logger.LogInformation($"[Mutation] CreateTransaction - Transaction between {cardName} with ({market.Name}) or an amount of a total {request.Transactions.Sum(x => x.Amount)} for product group(s) {request.Transactions.Select(x => x.ProductGroupId)}");

            if (beneficiary != null && !string.IsNullOrEmpty(beneficiary.Email))
            {
                try
                {
                    await mailer.Send(new TransactionBeneficiaryReceiptEmail(card.Beneficiary.Email.Trim(), market.Name,
                        card.Project.Name, card.Project.Url, request.Transactions.Sum(x => x.Amount), card.TotalFund(),
                        card.Funds.Select(x => new ProductGroupAvailableFund()
                        {
                            Fund = x.Amount,
                            Name = x.ProductGroup.Name == ProductGroupType.LOYALTY
                                ? "Carte-cadeau/Gift-card"
                                : x.ProductGroup.Name
                        })));
                }
                catch (Exception e)
                {
                    logger.LogError($"[Mutation] CreateTransaction - Could not send transaction confirmation email to ({cardName}) for transaction with ({market.Name}). Error message: {e.Message}");
                }
            }
            
            return new Payload() {
                Transaction = new PaymentTransactionGraphType(transaction)
            };
        }

        public void AddAmountToTransactionLog(PaymentTransaction paymentTransaction, Card card, Market market, Subscription subscription, ProductGroup productGroup, decimal amount)
        {
            var transactionLog = transactionLogs.FirstOrDefault(x => x.SubscriptionId == subscription?.Id);
            if (transactionLog == null)
            {
                transactionLog = new TransactionLog()
                {
                    Discriminator = TransactionLogDiscriminator.PaymentTransactionLog,
                    TransactionUniqueId = paymentTransaction.TransactionUniqueId,
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
                    SubscriptionId = subscription?.Id,
                    SubscriptionName = subscription?.Name,
                    ProjectId = card.ProjectId,
                    ProjectName = card.Project.Name,
                    TransactionInitiatorId = currentUser?.Id,
                    TransactionInitiatorFirstname = currentUser?.Profile.FirstName,
                    TransactionInitiatorLastname = currentUser?.Profile.LastName,
                    TransactionInitiatorEmail = currentUser?.Email,
                    TransactionLogProductGroups = new List<TransactionLogProductGroup>(),
                    InitiatedByProject = currentUser?.Type == UserType.ProjectManager
                };
                transactionLogs.Add(transactionLog);
            }

            transactionLog.TotalAmount += amount;
            
            var transactionLogProductGroup =
                transactionLog.TransactionLogProductGroups.FirstOrDefault(x => x.ProductGroupId == productGroup.Id);
            if (transactionLogProductGroup == null)
            {
                transactionLogProductGroup = new TransactionLogProductGroup()
                {
                    Amount = 0,
                    ProductGroupId = productGroup.Id,
                    ProductGroupName = productGroup.Name
                };
                transactionLog.TransactionLogProductGroups.Add(transactionLogProductGroup);
            }

            transactionLogProductGroup.Amount += amount;
        }

        [MutationInput]
        public class Input : HaveMarketId, IRequest<Payload>
        {
            public Id? CardId { get; set; }
            public string CardNumber { get; set; }
            public List<TransactionInput> Transactions { get; set; }
        }

        [InputType]
        public class TransactionInput
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
            public PaymentTransactionGraphType Transaction { get; set; }
        }

        public class CardNotFoundException : RequestValidationException { }
        public class MarketNotFoundException : RequestValidationException { }
        public class ProductGroupNotFoundException : RequestValidationException { }
        public class CardCantBeUsedInMarketException : RequestValidationException { }
        public class CardDontHaveFundType : RequestValidationException { }
        public class NotEnoughtFundException : RequestValidationException { }
    }
}
