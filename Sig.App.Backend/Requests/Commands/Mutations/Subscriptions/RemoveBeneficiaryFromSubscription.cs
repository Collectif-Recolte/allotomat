using System.Collections.Generic;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Enums;

namespace Sig.App.Backend.Requests.Commands.Mutations.Subscriptions
{
    public class RemoveBeneficiaryFromSubscription : AsyncRequestHandler<RemoveBeneficiaryFromSubscription.Input>
    {
        private readonly ILogger<RemoveBeneficiaryFromSubscription> logger;
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly IHttpContextAccessor httpContextAccessor;

        public RemoveBeneficiaryFromSubscription(ILogger<RemoveBeneficiaryFromSubscription> logger, AppDbContext db, IClock clock, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.db = db;
            this.clock = clock;
            this.httpContextAccessor = httpContextAccessor;
        }

        protected override async Task Handle(Input request, CancellationToken cancellationToken)
        {
            var subscriptionId = request.SubscriptionId.LongIdentifierForType<Subscription>();
            var subscription = await db.Subscriptions.Include(x => x.Types).ThenInclude(x => x.ProductGroup)
                .Include(x => x.Beneficiaries)
                .ThenInclude(x => x.BudgetAllowance)
                .FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

            if (subscription == null) throw new SubscriptionNotFoundException();

            var beneficiaryId = request.BeneficiaryId.LongIdentifierForType<Beneficiary>();
            var beneficiary = await db.Beneficiaries.Include(x => x.Organization).ThenInclude(x => x.Project).Include(x => x.Card)
                .FirstOrDefaultAsync(x => x.Id == beneficiaryId, cancellationToken);

            if (beneficiary == null) throw new BeneficiaryNotFoundException();

            var subscriptionBeneficiary = subscription.Beneficiaries.FirstOrDefault(x => x.BeneficiaryId == beneficiaryId);
            if (subscriptionBeneficiary == null) throw new BeneficiaryNotInSubscriptionException();
            
            var today = clock.GetCurrentInstant().InUtc().ToDateTimeUtc();
            var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            var currentUser = db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId);

            beneficiary.Subscriptions.Remove(subscriptionBeneficiary);

            var paymentsRemaining = subscription.GetPaymentRemaining(clock);
            var subscriptionTypes = subscription.Types.Where(x => x.BeneficiaryTypeId == subscriptionBeneficiary.BeneficiaryTypeId).ToList();
            var totalRefund = paymentsRemaining * subscriptionTypes.Sum(x => x.Amount);
            subscriptionBeneficiary.BudgetAllowance.AvailableFund += totalRefund;

            var transactionLogProductGroups = new List<TransactionLogProductGroup>();
            foreach (var productGroup in subscriptionTypes.GroupBy(x => x.ProductGroupId))
            {
                var currentProductGroup = productGroup.First().ProductGroup;
                transactionLogProductGroups.Add(new TransactionLogProductGroup()
                {
                    Amount = paymentsRemaining * productGroup.Sum(x => x.Amount),
                    ProductGroupId = currentProductGroup.Id,
                    ProductGroupName = currentProductGroup.Name
                });
            }
            
            db.TransactionLogs.Add(new TransactionLog()
            {
                Discriminator = TransactionLogDiscriminator
                    .RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog,
                CreatedAtUtc = today,
                TotalAmount = totalRefund,
                CardProgramCardId = beneficiary.Card?.ProgramCardId,
                CardNumber = beneficiary.Card?.CardNumber,
                BeneficiaryId = beneficiary.Id,
                BeneficiaryID1 = beneficiary.ID1,
                BeneficiaryID2 = beneficiary.ID2,
                BeneficiaryFirstname = beneficiary.Firstname,
                BeneficiaryLastname = beneficiary.Lastname,
                BeneficiaryEmail = beneficiary.Email,
                BeneficiaryPhone = beneficiary.Phone,
                BeneficiaryIsOffPlatform = beneficiary is OffPlatformBeneficiary,
                BeneficiaryTypeId = beneficiary.BeneficiaryTypeId,
                OrganizationId = beneficiary.OrganizationId,
                OrganizationName = beneficiary.Organization.Name,
                SubscriptionId = subscription?.Id,
                SubscriptionName = subscription?.Name,
                ProjectId = beneficiary.Organization.ProjectId,
                ProjectName = beneficiary.Organization.Project.Name,
                TransactionLogProductGroups = transactionLogProductGroups,
                TransactionInitiatorId = currentUserId,
                TransactionInitiatorFirstname = currentUser?.Profile.FirstName,
                TransactionInitiatorLastname = currentUser?.Profile.LastName,
                TransactionInitiatorEmail = currentUser?.Email
            });
            
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"Beneficiary {beneficiary.Firstname} {beneficiary.Lastname} remove from subscription {subscription.Name}");
        }

        public class SubscriptionNotFoundException : RequestValidationException { }
        public class BeneficiaryNotFoundException : RequestValidationException { }
        public class BeneficiaryNotInSubscriptionException : RequestValidationException { }

        [MutationInput]
        public class Input : IRequest, IHaveSubscriptionId, IHaveBeneficiaryId
        {
            public Id BeneficiaryId { get; set; }
            public Id SubscriptionId { get; set; }
        }
    }
}
