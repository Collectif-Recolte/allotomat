using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Requests.Queries.Organizations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Organizations
{
    public class DeleteOrganization : IRequestHandler<DeleteOrganization.Input>
    {
        private readonly ILogger<DeleteOrganization> logger;
        private readonly UserManager<AppUser> userManager;
        private readonly AppDbContext db;
        private readonly IMediator mediator;

        public DeleteOrganization(ILogger<DeleteOrganization> logger, AppDbContext db, UserManager<AppUser> userManager, IMediator mediator)
        {
            this.logger = logger;
            this.userManager = userManager;
            this.db = db;
            this.mediator = mediator;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] DeleteOrganization({request.OrganizationId})");
            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations
                .Include(x => x.Beneficiaries).ThenInclude(x => x.Card).ThenInclude(x => x.Transactions)
                .Include(x => x.Beneficiaries).ThenInclude(x => x.Subscriptions)
                .FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null)
            {
                logger.LogWarning("[Mutation] DeleteOrganization - OrganizationNotFoundException");
                throw new OrganizationNotFoundException();
            }

            var organizationManagers = await mediator.Send(new GetOrganizationManagers.Query
            {
                OrganizationId = organizationId
            });

            if (organizationManagers != null)
            {
                foreach (var manager in organizationManagers)
                {
                    await userManager.RemoveClaimAsync(manager, new Claim(AppClaimTypes.OrganizationManagerOf, organizationId.ToString()));
                    logger.LogInformation($"[Mutation] DeleteOrganization - Remove claim from manager {manager.Email}");
                }
            }

            var refundTransactionsProductGroup = new List<RefundTransactionProductGroup>();
            var paymentTransactionsProductGroup = new List<PaymentTransactionProductGroup>();
            var paymentTransactionAddingFundTransactions = new List<PaymentTransactionAddingFundTransaction>();
            var transactionsToRemove = new List<Transaction>();

            foreach (var transaction in organization.Beneficiaries.SelectMany(b => b.Card.Transactions))
            {
               var type = transaction.GetType();
                if (type == typeof(PaymentTransaction))
                {
                    var paymentTransaction = await db.Transactions.OfType<PaymentTransaction>()
                        .Include(x => x.RefundTransactions)
                        .Include(x => x.Transactions).ThenInclude(x => x.PaymentTransactionAddingFundTransactions)
                        .Include(x => x.TransactionByProductGroups).ThenInclude(x => x.RefundTransactionsProductGroup)
                        .Include(x => x.PaymentTransactionAddingFundTransactions).ThenInclude(x => x.AddingFundTransaction).ThenInclude(x => x.PaymentTransactionAddingFundTransactions)
                        .Include(x => x.PaymentTransactionAddingFundTransactions).ThenInclude(x => x.AddingFundTransaction).ThenInclude(x => x.Transactions)
                        .FirstAsync(x => x.Id == transaction.Id);

                    foreach (var transactionProductGroup in paymentTransaction.TransactionByProductGroups)
                    {
                        paymentTransactionsProductGroup.Add(transactionProductGroup);
                        foreach (var refundTransactionProductGroup in transactionProductGroup.RefundTransactionsProductGroup)
                        {
                            refundTransactionsProductGroup.Add(refundTransactionProductGroup);
                        }
                    }

                    foreach (var paymentTransactionAddingFundTransaction in paymentTransaction.PaymentTransactionAddingFundTransactions)
                    {
                        paymentTransactionAddingFundTransactions.Add(paymentTransactionAddingFundTransaction);
                    }

                }
                transactionsToRemove.Add(transaction);
            }

            db.PaymentTransactionAddingFundTransactions.RemoveRange(paymentTransactionAddingFundTransactions);

            db.Transactions.RemoveRange(transactionsToRemove);
            db.RefundTransactionProductGroups.RemoveRange(refundTransactionsProductGroup);
            db.PaymentTransactionProductGroups.RemoveRange(paymentTransactionsProductGroup);

            var transactionLogs = db.TransactionLogs.Include(x => x.TransactionLogProductGroups).Where(x => x.OrganizationId == organizationId);
            db.TransactionLogProductGroups.RemoveRange(transactionLogs.SelectMany(x => x.TransactionLogProductGroups));
            db.TransactionLogs.RemoveRange(transactionLogs);

            db.SubscriptionBeneficiaries.RemoveRange(organization.Beneficiaries.SelectMany(x => x.Subscriptions));
            db.Beneficiaries.RemoveRange(organization.Beneficiaries);
            db.Organizations.Remove(organization);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (Exception e)
            {
                logger.LogError(e, "[Mutation] DeleteOrganization - DbUpdateConcurrencyException");
                throw;
            }
            logger.LogInformation($"[Mutation] DeleteOrganization - Organization deleted ({organizationId}, {organization.Name})");
        }

        [MutationInput]
        public class Input : HaveOrganizationId, IRequest { }

        public class OrganizationNotFoundException : RequestValidationException { }
    }
}