using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Plugins.GraphQL;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Sig.App.Backend.Constants;
using System.Linq;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.Requests.Queries.Markets;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Gql.Bases;
using System;
using System.Collections.Generic;

namespace Sig.App.Backend.Requests.Commands.Mutations.Markets
{
    public class DeleteMarket : IRequestHandler<DeleteMarket.Input>
    {
        private readonly ILogger<DeleteMarket> logger;
        private readonly UserManager<AppUser> userManager;
        private readonly AppDbContext db;
        private readonly IMediator mediator;

        public DeleteMarket(ILogger<DeleteMarket> logger, AppDbContext db, UserManager<AppUser> userManager, IMediator mediator)
        {
            this.logger = logger;
            this.userManager = userManager;
            this.db = db;
            this.mediator = mediator;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] DeleteMarket({request.MarketId})");
            var marketId = request.MarketId.LongIdentifierForType<Market>();
            var market = await db.Markets
                .Include(x => x.CashRegisters).ThenInclude(x => x.MarketGroups)
                .Include(x => x.MarketGroups)
                .Include(x => x.Projects)
                .FirstOrDefaultAsync(x => x.Id == marketId, cancellationToken);

            if (market == null)
            {
                logger.LogWarning("[Mutation] DeleteMarket - MarketNotFoundException");
                throw new MarketNotFoundException();
            }

            var marketManagers = await mediator.Send(new GetMarketManagers.Query
            {
                MarketId = marketId
            });

            if (marketManagers != null)
            {
                foreach (var manager in marketManagers)
                {
                    await userManager.RemoveClaimAsync(manager, new Claim(AppClaimTypes.MarketManagerOf, marketId.ToString()));
                    logger.LogInformation($"[Mutation] DeleteMarket - Remove claim from manager {manager.Email}");
                }
            }

            var transactions = await db.Transactions.OfType<PaymentTransaction>()
                .Include(x => x.RefundTransactions)
                .Include(x => x.Transactions).ThenInclude(x => x.PaymentTransactionAddingFundTransactions)
                .Include(x => x.TransactionByProductGroups).ThenInclude(x => x.RefundTransactionsProductGroup)
                .Include(x => x.PaymentTransactionAddingFundTransactions).ThenInclude(x => x.AddingFundTransaction).ThenInclude(x => x.PaymentTransactionAddingFundTransactions)
                .Include(x => x.PaymentTransactionAddingFundTransactions).ThenInclude(x => x.AddingFundTransaction).ThenInclude(x => x.Transactions)
                .Where(x => x.MarketId == marketId).ToListAsync();
            var refundTransactionsProductGroup = new List<RefundTransactionProductGroup>();
            var paymentTransactionsProductGroup = new List<PaymentTransactionProductGroup>();
            var paymentTransactionAddingFundTransactions = new List<PaymentTransactionAddingFundTransaction>();
            var transactionsToRemove = new List<Transaction>();

            foreach (var transaction in transactions)
            {
                foreach (var transactionProductGroup in transaction.TransactionByProductGroups)
                {
                    paymentTransactionsProductGroup.Add(transactionProductGroup);
                    transactionProductGroup.ProductGroup = null;
                    transactionProductGroup.PaymentTransaction = null;
                    foreach (var refundTransactionProductGroup in transactionProductGroup.RefundTransactionsProductGroup)
                    {
                        refundTransactionsProductGroup.Add(refundTransactionProductGroup);
                        refundTransactionProductGroup.ProductGroup = null;
                        refundTransactionProductGroup.PaymentTransactionProductGroup = null;
                        refundTransactionProductGroup.RefundTransaction = null;
                    }
                    transactionProductGroup.RefundTransactionsProductGroup = null;
                }
                transaction.TransactionByProductGroups = null;
                
                foreach (var paymentTransactionAddingFundTransaction in transaction.PaymentTransactionAddingFundTransactions)
                {
                    paymentTransactionAddingFundTransaction.PaymentTransaction = null;
                    paymentTransactionAddingFundTransaction.AddingFundTransaction.PaymentTransactionAddingFundTransactions.Remove(paymentTransactionAddingFundTransaction);
                    paymentTransactionAddingFundTransaction.AddingFundTransaction.Transactions.Remove(transaction);
                    paymentTransactionAddingFundTransaction.AddingFundTransaction = null;
                    paymentTransactionAddingFundTransactions.Add(paymentTransactionAddingFundTransaction);
                }

                foreach (var refundTransaction in transaction.RefundTransactions)
                {
                    refundTransaction.InitialTransaction = null;
                    refundTransaction.RefundByProductGroups = null;
                    transactionsToRemove.Add(refundTransaction);
                }

                transaction.Card = null;
                transaction.RefundTransactions = null;
                transaction.Beneficiary = null;
                transaction.Market = null;
                transaction.Organization = null;
                transaction.Transactions = null;
                transaction.CashRegister = null;
                transaction.PaymentTransactionAddingFundTransactions = null;
                transactionsToRemove.Add(transaction);
            }

            db.PaymentTransactionAddingFundTransactions.RemoveRange(paymentTransactionAddingFundTransactions);
            
            db.Transactions.RemoveRange(transactionsToRemove);
            db.RefundTransactionProductGroups.RemoveRange(refundTransactionsProductGroup);
            db.PaymentTransactionProductGroups.RemoveRange(paymentTransactionsProductGroup);

            var transactionLogs = db.TransactionLogs.Include(x => x.TransactionLogProductGroups).Where(x => x.MarketId == marketId);
            db.TransactionLogProductGroups.RemoveRange(transactionLogs.SelectMany(x => x.TransactionLogProductGroups));
            db.TransactionLogs.RemoveRange(transactionLogs);

            db.CashRegisters.RemoveRange(market.CashRegisters);
            db.MarketGroupMarkets.RemoveRange(market.MarketGroups);
            db.CashRegisterMarketGroups.RemoveRange(market.CashRegisters.SelectMany(x => x.MarketGroups));

            db.ProjectMarkets.RemoveRange(market.Projects);

            db.Markets.Remove(market);

            await db.SaveChangesAsync();
            logger.LogInformation($"[Mutation] DeleteMarket - Market deleted ({marketId}, {market.Name})");
        }

        [MutationInput]
        public class Input : HaveMarketId, IRequest {}

        public class MarketNotFoundException : RequestValidationException { }
    }
}
