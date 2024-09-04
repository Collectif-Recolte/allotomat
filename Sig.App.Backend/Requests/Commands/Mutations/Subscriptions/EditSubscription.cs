using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Subscriptions
{
    public class EditSubscription : IRequestHandler<EditSubscription.Input, EditSubscription.Payload>
    {
        private readonly ILogger<EditSubscription> logger;
        private readonly AppDbContext db;

        public EditSubscription(ILogger<EditSubscription> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] EditSubscription({request.SubscriptionId}, {request.Name}, {request.StartDate}, {request.EndDate}, {request.MonthlyPaymentMoment}, {request.IsSubscriptionPaymentBasedCardUsage}, {request.MaxNumberOfPayments}, {request.IsFundsAccumulable}, {request.FundsExpirationDate}, {request.TriggerFundExpiration}, {request.NumberDaysUntilFundsExpire}, {request.Types})");
            var subscriptionId = request.SubscriptionId.LongIdentifierForType<Subscription>();
            var subscription = await db.Subscriptions.Include(x => x.Types).FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

            if (subscription == null)
            {
                logger.LogWarning("[Mutation] EditSubscription - SubscriptionNotFoundException");
                throw new SubscriptionNotFoundException();
            }

            var beneficiaryTypeIds = request.Types.Select(x => x.BeneficiaryTypeId.ToString() + x.ProductGroupId.ToString());
            if (!beneficiaryTypeIds.Any())
            {
                logger.LogWarning("[Mutation] EditSubscription - SubscriptionTypesCantBeEmpty");
                throw new SubscriptionTypesCantBeEmpty();
            }
            if (beneficiaryTypeIds.Count() != beneficiaryTypeIds.Distinct().Count())
            {
                logger.LogWarning("[Mutation] EditSubscription - BeneficiaryTypeCanOnlyBeAssignOnce");
                throw new BeneficiaryTypeCanOnlyBeAssignOnce();
            }

            if (db.SubscriptionBeneficiaries.Where(x => x.SubscriptionId == subscriptionId).Any())
            {
                logger.LogWarning("[Mutation] EditSubscription - CantEditSubscriptionWithBeneficiaries");
                throw new CantEditSubscriptionWithBeneficiaries();
            }

            if (request.StartDate > request.EndDate)
            {
                logger.LogWarning("[Mutation] EditSubscription - EndDateMustBeAfterStartDateException");
                throw new EndDateMustBeAfterStartDateException();
            }

            if (request.IsSubscriptionPaymentBasedCardUsage && (!request.MaxNumberOfPayments.IsSet() || request.MaxNumberOfPayments.Value <= 0))
            {
                logger.LogWarning("[Mutation] EditSubscription - MaxNumberOfPaymentsCantBeZeroException");
                throw new MaxNumberOfPaymentsCantBeZeroException();
            }

            if (request.TriggerFundExpiration == FundsExpirationTrigger.NumberOfDays && (!request.NumberDaysUntilFundsExpire.IsSet() || request.NumberDaysUntilFundsExpire.Value <= 0))
            {
                logger.LogWarning("[Mutation] EditSubscription - NumberDaysUntilFundsExpireCantBeZeroException");
                throw new NumberDaysUntilFundsExpireCantBeZeroException();
            }

            subscription.Name = request.Name.Trim();
            subscription.MonthlyPaymentMoment = request.MonthlyPaymentMoment;
            subscription.StartDate = request.StartDate.AtMidnight().InUtc().ToDateTimeUtc();
            subscription.EndDate = request.EndDate.AtMidnight().InUtc().ToDateTimeUtc();
            subscription.FundsExpirationDate = request.FundsExpirationDate.IfSet(x => subscription.FundsExpirationDate = x.AtMidnight().InUtc().ToDateTimeUtc());
            subscription.IsFundsAccumulable = request.IsFundsAccumulable;
            subscription.IsSubscriptionPaymentBasedCardUsage = request.IsSubscriptionPaymentBasedCardUsage;
            subscription.TriggerFundExpiration = request.TriggerFundExpiration;

            if (request.MaxNumberOfPayments.IsSet())
            {
                subscription.MaxNumberOfPayments = request.MaxNumberOfPayments.Value;
            }
            else
            {
                subscription.MaxNumberOfPayments = null;
            }

            if (request.NumberDaysUntilFundsExpire.IsSet())
            {
                subscription.NumberDaysUntilFundsExpire = request.NumberDaysUntilFundsExpire.Value;
            }
            else
            {
                subscription.NumberDaysUntilFundsExpire = null;
            }

            UpdateTypes(subscription, request.Types);

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] EditSubscription - Subscription {subscription.Name} ({subscription.Id}) updated");

            return new Payload()
            {
                Subscription = new SubscriptionGraphType(subscription)
            };
        }

        private void UpdateTypes(Subscription subscription, IList<EditSubscriptionTypeInput> types)
        {
            subscription.Types.AddRemoveUpdate(
                types,
                (type, input) => type.Id == input.OriginalId?.LongIdentifierForType<SubscriptionType>(),
                (type, input) => {
                    input.Amount = type.Amount;
                    input.BeneficiaryTypeId = type.BeneficiaryTypeId.LongIdentifierForType<BeneficiaryType>();
                    input.ProductGroupId = type.ProductGroupId.LongIdentifierForType<ProductGroup>();
                });
        }

        [MutationInput]
        public class Input : HaveSubscriptionId, IRequest<Payload>
        {
            public string Name { get; set; }
            public LocalDate StartDate { get; set; }
            public LocalDate EndDate { get; set; }
            public SubscriptionMonthlyPaymentMoment MonthlyPaymentMoment { get; set; }
            public bool IsSubscriptionPaymentBasedCardUsage { get; set; }
            public Maybe<int> MaxNumberOfPayments { get; set; }
            public bool IsFundsAccumulable { get; set; }
            public Maybe<LocalDate> FundsExpirationDate { get; set; }
            public FundsExpirationTrigger TriggerFundExpiration { get; set; }
            public Maybe<int> NumberDaysUntilFundsExpire { get; set; }
            public List<EditSubscriptionTypeInput> Types { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public SubscriptionGraphType Subscription { get; set; }
        }

        [InputType]
        public class EditSubscriptionTypeInput
        {
            public Id? OriginalId { get; set; }
            public decimal Amount { get; set; }
            public Id BeneficiaryTypeId { get; set; }
            public Id ProductGroupId { get; set; }
        }

        public class SubscriptionNotFoundException : RequestValidationException { }
        public class SubscriptionTypesCantBeEmpty : RequestValidationException { }
        public class BeneficiaryTypeCanOnlyBeAssignOnce : RequestValidationException { }
        public class EndDateMustBeAfterStartDateException : RequestValidationException { }
        public class CantEditSubscriptionWithBeneficiaries : RequestValidationException { }
        public class MaxNumberOfPaymentsCantBeZeroException : RequestValidationException { }
        public class NumberDaysUntilFundsExpireCantBeZeroException : RequestValidationException { }
    }
}
