using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Plugins.MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.Beneficiaries
{
    public class ForecastAddingFundTransactionForSubscriptionByBeneficiary : IRequestHandler<ForecastAddingFundTransactionForSubscriptionByBeneficiary.Input, ForecastAddingFundTransactionForSubscriptionByBeneficiary.AddingFundTransactionForSubscriptionByBeneficiaryPayload>
    {
        private IClock clock;
        private readonly AppDbContext db;

        public ForecastAddingFundTransactionForSubscriptionByBeneficiary(IClock clock, AppDbContext db)
        {
            this.clock = clock;
            this.db = db;
        }

        public async Task<AddingFundTransactionForSubscriptionByBeneficiaryPayload> Handle(Input request, CancellationToken cancellationToken)
        {
            var today = clock.GetCurrentInstant().ToDateTimeUtc();

            var subscriptionId = request.SubscriptionId.LongIdentifierForType<Subscription>();
            var subscription = await db.Subscriptions.Include(x => x.Project).FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);

            if (subscription == null) throw new SubscriptionNotFoundException();

            var beneficiaryIds = request.Beneficiaries.Select(x => x.LongIdentifierForType<Beneficiary>()).ToList();
            var beneficiaries = await db.Beneficiaries.Where(x => beneficiaryIds.Contains(x.Id)).ToListAsync();

            if (beneficiaryIds.Count != beneficiaries.Count)
            {
                throw new BeneficiaryNotFoundException();
            }

            var results = new List<AddingFundTransactionForSubscriptionByBeneficiaryItem>();

            foreach (var item in beneficiaryIds)
            {
                var transactionCount = await db.Transactions.OfType<SubscriptionAddingFundTransaction>().Where(x => x.BeneficiaryId == item && x.SubscriptionType.SubscriptionId == subscriptionId).CountAsync();
                
                results.Add(new AddingFundTransactionForSubscriptionByBeneficiaryItem()
                {
                    BeneficiaryId = Id.New<Beneficiary>(item),
                    Count = transactionCount
                });
            }

            return new AddingFundTransactionForSubscriptionByBeneficiaryPayload()
            {
                Beneficiaries = results
            };
        }

        public class Input : HaveSubscriptionId, IRequest<AddingFundTransactionForSubscriptionByBeneficiaryPayload>
        {
            public IEnumerable<Id> Beneficiaries { get; set; }
        }

        public class AddingFundTransactionForSubscriptionByBeneficiaryPayload
        {
            public IEnumerable<AddingFundTransactionForSubscriptionByBeneficiaryItem> Beneficiaries { get; set; }
        }

        public class AddingFundTransactionForSubscriptionByBeneficiaryItem
        {
            public Id BeneficiaryId { get; set; }
            public int Count { get; set; }
        }

        public class SubscriptionNotFoundException : RequestValidationException { }
        public class BeneficiaryNotFoundException : RequestValidationException { }
    }
}