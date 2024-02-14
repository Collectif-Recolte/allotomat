using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Plugins.GraphQL;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.Services.Mailer;
using Microsoft.AspNetCore.Identity;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.DbModel.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Requests.Queries.Projects;
using System.Security.Claims;
using Sig.App.Backend.Constants;
using NodaTime;
using System.Linq;
using Sig.App.Backend.DbModel.Entities.Transactions;
using System.Collections.Generic;
using Sig.App.Backend.Requests.Queries.Organizations;
using Sig.App.Backend.Gql.Bases;

namespace Sig.App.Backend.Requests.Commands.Mutations.Projects
{
    public class DeleteProject : IRequestHandler<DeleteProject.Input>
    {
        private readonly ILogger<DeleteProject> logger;
        private readonly IMailer mailer;
        private readonly UserManager<AppUser> userManager;
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly IMediator mediator;

        public DeleteProject(ILogger<DeleteProject> logger, AppDbContext db, UserManager<AppUser> userManager, IMailer mailer, IMediator mediator, IClock clock)
        {
            this.logger = logger;
            this.userManager = userManager;
            this.mailer = mailer;
            this.db = db;
            this.mediator = mediator;
            this.clock = clock;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] DeleteProject({request.ProjectId})");
            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var project = await db.Projects
                .Include(x => x.Markets).ThenInclude(x => x.Market)
                .Include(x => x.ProductGroups).ThenInclude(x => x.Types)
                .Include(x => x.Subscriptions).ThenInclude(x => x.Beneficiaries)
                .Include(x => x.Subscriptions).ThenInclude(x => x.BudgetAllowances)
                .Include(x => x.Organizations).ThenInclude(x => x.Beneficiaries)
                .Include(x => x.Cards).ThenInclude(x => x.Transactions)
                .Include(x => x.Cards).ThenInclude(x => x.Funds)
                .FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

            if (project == null) throw new ProjectNotFoundException();

            if (HaveAnyActiveSubscription(project))
            {
                throw new ProjectCantHaveActiveSubscription();
            }

            var projectManagers = await mediator.Send(new GetProjectProjectManagers.Query
            {
                ProjectId = projectId
            });

            if (projectManagers != null) {
                foreach (var manager in projectManagers)
                {
                    await userManager.RemoveClaimAsync(manager, new Claim(AppClaimTypes.ProjectManagerOf, projectId.ToString()));
                    await userManager.DeleteAsync(manager);
                }
            }

            foreach (var organization in project.Organizations)
            {
                var organizationManagers = await mediator.Send(new GetOrganizationManagers.Query
                {
                    OrganizationId = organization.Id
                });

                if (organizationManagers != null)
                {
                    foreach (var manager in organizationManagers)
                    {
                        await userManager.RemoveClaimAsync(manager, new Claim(AppClaimTypes.OrganizationManagerOf, organization.Id.ToString()));
                        await userManager.DeleteAsync(manager);
                    }
                }
            }

            var refundTransactionsProductGroup = new List<RefundTransactionProductGroup>();
            var paymentTransactionsProductGroup = new List<PaymentTransactionProductGroup>();
            foreach (var transaction in project.Cards.SelectMany(x => x.Transactions))
            {
                var type = transaction.GetType();
                if (type == typeof(PaymentTransaction))
                {
                    var paymentTransaction = db.Transactions.OfType<PaymentTransaction>().Include(x => x.TransactionByProductGroups).ThenInclude(x => x.RefundTransactionsProductGroup).First(x => x.Id == transaction.Id);
                    paymentTransaction.Card = null;
                    paymentTransaction.RefundTransactions = null;
                    paymentTransaction.Beneficiary = null;
                    paymentTransaction.Market = null;
                    paymentTransaction.Organization = null;
                    paymentTransaction.Transactions = null;
                    foreach (var transactionProductGroup in paymentTransaction.TransactionByProductGroups)
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
                    paymentTransaction.TransactionByProductGroups = null;
                }
                else if (type == typeof(SubscriptionAddingFundTransaction))
                {
                    var subscriptionAddingFundTransaction = transaction as SubscriptionAddingFundTransaction;
                    subscriptionAddingFundTransaction.ProductGroup = null;
                    subscriptionAddingFundTransaction.Card = null;
                    subscriptionAddingFundTransaction.Beneficiary = null;
                    subscriptionAddingFundTransaction.ExpireFundTransaction = null;
                    subscriptionAddingFundTransaction.Organization = null;
                    subscriptionAddingFundTransaction.SubscriptionType = null;
                    subscriptionAddingFundTransaction.Transactions = null;
                }
                else if (type == typeof(RefundTransaction))
                {
                    var refundTransaction = transaction as RefundTransaction;
                    refundTransaction.InitialTransaction = null;
                    refundTransaction.Organization = null;
                    refundTransaction.Beneficiary = null;
                    refundTransaction.Card = null;
                    refundTransaction.RefundByProductGroups = null;
                }
                else if(type == typeof(OffPlatformAddingFundTransaction) || type == typeof(LoyaltyAddingFundTransaction))
                {
                    var addingFundTransaction = transaction as AddingFundTransaction;
                    addingFundTransaction.Card = null;
                    addingFundTransaction.ProductGroup = null;
                    addingFundTransaction.Beneficiary = null;
                    addingFundTransaction.ExpireFundTransaction = null;
                    addingFundTransaction.Organization = null;
                    addingFundTransaction.Transactions = null;
                }
                else if (type == typeof(ManuallyAddingFundTransaction))
                {
                    var manuallyAddingFundTransaction = transaction as ManuallyAddingFundTransaction;
                    manuallyAddingFundTransaction.Card = null;
                    manuallyAddingFundTransaction.ProductGroup = null;
                    manuallyAddingFundTransaction.Beneficiary = null;
                    manuallyAddingFundTransaction.ExpireFundTransaction = null;
                    manuallyAddingFundTransaction.Organization = null;
                    manuallyAddingFundTransaction.Transactions = null;
                    manuallyAddingFundTransaction.Subscription = null;
                }
                else if (type == typeof(ExpireFundTransaction))
                {
                    var expireFundTransaction = transaction as ExpireFundTransaction;
                    expireFundTransaction.Card = null;
                    expireFundTransaction.ProductGroup = null;
                    expireFundTransaction.AddingFundTransaction = null;
                    expireFundTransaction.Beneficiary = null;
                    expireFundTransaction.ExpiredSubscription = null;
                    expireFundTransaction.Organization = null;
                }
            }

            var transactions = project.Cards.SelectMany(x => x.Transactions).ToList();
            var transactionLogs = db.TransactionLogs.Include(x => x.TransactionLogProductGroups).Where(x => x.ProjectId == projectId);
            db.TransactionLogProductGroups.RemoveRange(transactionLogs.SelectMany(x => x.TransactionLogProductGroups));
            db.TransactionLogs.RemoveRange(transactionLogs);
            db.Transactions.RemoveRange(transactions);
            db.RefundTransactionProductGroups.RemoveRange(refundTransactionsProductGroup);
            db.PaymentTransactionProductGroups.RemoveRange(paymentTransactionsProductGroup);
            db.Cards.RemoveRange(project.Cards);
            db.SubscriptionBeneficiaries.RemoveRange(project.Subscriptions.SelectMany(x => x.Beneficiaries));
            db.Beneficiaries.RemoveRange(project.Organizations.SelectMany(x => x.Beneficiaries));
            db.SubscriptionTypes.RemoveRange(project.ProductGroups.SelectMany(x => x.Types));
            db.Funds.RemoveRange(project.Cards.SelectMany(x => x.Funds));
            db.ProductGroups.RemoveRange(project.ProductGroups);
            db.BudgetAllowances.RemoveRange(project.Subscriptions.SelectMany(x => x.BudgetAllowances));
            db.Subscriptions.RemoveRange(project.Subscriptions);
            db.ProjectMarkets.RemoveRange(project.Markets);
            db.Organizations.RemoveRange(project.Organizations);

            db.Projects.Remove(project);

            await db.SaveChangesAsync();

            logger.LogInformation($"Project deleted ({projectId}, {project.Name})");
        }

        private bool HaveAnyActiveSubscription(Project project)
        {
            var haveAnyActiveSubscription = false;
            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            foreach (var subscription in project.Subscriptions)
            {
                if (subscription.StartDate <= today && subscription.EndDate >= today)
                {
                    haveAnyActiveSubscription = true;
                }
            }

            return haveAnyActiveSubscription;
        }

        [MutationInput]
        public class Input : HaveProjectId, IRequest {}

        public class ProjectNotFoundException : RequestValidationException { }
        public class ProjectCantHaveActiveSubscription : RequestValidationException { }
    }
}
