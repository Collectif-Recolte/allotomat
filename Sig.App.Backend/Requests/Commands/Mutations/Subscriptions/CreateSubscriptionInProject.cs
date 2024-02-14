using DocumentFormat.OpenXml.Presentation;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
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
    public class CreateSubscriptionInProject : IRequestHandler<CreateSubscriptionInProject.Input, CreateSubscriptionInProject.Payload>
    {
        private readonly ILogger<CreateSubscriptionInProject> logger;
        private readonly AppDbContext db;

        public CreateSubscriptionInProject(ILogger<CreateSubscriptionInProject> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] CreateSubscriptionInProject({request.ProjectId}, {request.Name}, {request.MonthlyPaymentMoment}, {request.StartDate}, {request.EndDate}, {request.FundsExpirationDate}, {request.Types}, {request.IsFundsAccumulable})");
            if (request.StartDate > request.EndDate) throw new EndDateMustBeAfterStartDateException();

            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var project = await db.Projects.Include(x => x.BeneficiaryTypes).Include(x => x.ProductGroups).FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

            if (project == null) throw new ProjectNotFoundException();

            var beneficiaryTypeIds = request.Types.Select(x => x.BeneficiaryTypeId);
            if (!beneficiaryTypeIds.Any()) throw new SubscriptionTypesCantBeEmpty();

            var subscription = new Subscription()
            {
                Name = request.Name,
                MonthlyPaymentMoment = request.MonthlyPaymentMoment,
                ProjectId = projectId,
                StartDate = request.StartDate.AtMidnight().InUtc().ToDateTimeUtc(),
                EndDate = request.EndDate.AtMidnight().InUtc().ToDateTimeUtc(),
                IsFundsAccumulable = request.IsFundsAccumulable,
                Types = new List<SubscriptionType>()
            };

            request.FundsExpirationDate.IfSet(x => subscription.FundsExpirationDate = x.AtMidnight().InUtc().ToDateTimeUtc());

            db.Subscriptions.Add(subscription);

            foreach (var type in request.Types)
            {
                var beneficiaryTypeId = type.BeneficiaryTypeId.LongIdentifierForType<BeneficiaryType>();
                var beneficiaryType = project.BeneficiaryTypes.First(x => x.Id == beneficiaryTypeId);

                if (beneficiaryType == null) throw new BeneficiaryTypeNotFoundException();

                var productGroupId = type.ProductGroupId.LongIdentifierForType<ProductGroup>();
                var productGroup = project.ProductGroups.First(x => x.Id == productGroupId);

                if (productGroup == null) throw new ProductGroupNotFoundException();

                if (subscription.Types.Any(x => x.BeneficiaryTypeId == beneficiaryTypeId && x.ProductGroupId == productGroupId)) throw new CantHaveMultipleBeneficiaryTypeAndProductGroupInSubscriptionException();

                subscription.Types.Add(new SubscriptionType()
                {
                    Amount = type.Amount,
                    Subscription = subscription,
                    BeneficiaryType = beneficiaryType,
                    ProductGroup = productGroup
                });
            }

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"New subscription created {subscription.Name} ({subscription.Id})");

            return new Payload()
            {
                Subscription = new SubscriptionGraphType(subscription)
            };
        }

        [MutationInput]
        public class Input : HaveProjectId, IRequest<Payload>
        {
            public string Name { get; set; }
            public SubscriptionMonthlyPaymentMoment MonthlyPaymentMoment { get; set; }
            public LocalDate StartDate { get; set; }
            public LocalDate EndDate { get; set; }
            public Maybe<LocalDate> FundsExpirationDate { get; set; }
            public List<SubscriptionTypeInput> Types { get; set; }
            public bool IsFundsAccumulable { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public SubscriptionGraphType Subscription { get; set; }
        }

        [InputType]
        public class SubscriptionTypeInput
        {
            public Id ProductGroupId { get; set; }
            public decimal Amount { get; set; }
            public Id BeneficiaryTypeId { get; set; }

            public override string ToString()
            {
                return $"{ProductGroupId}, {Amount}, {BeneficiaryTypeId}";
            }
        }

        public class ProjectNotFoundException : RequestValidationException { }
        public class EndDateMustBeAfterStartDateException : RequestValidationException { }
        public class SubscriptionTypesCantBeEmpty : RequestValidationException { }
        public class BeneficiaryTypeNotFoundException : RequestValidationException { }
        public class ProductGroupNotFoundException : RequestValidationException { }
        public class CantHaveMultipleBeneficiaryTypeAndProductGroupInSubscriptionException : RequestValidationException { }
    }
}
