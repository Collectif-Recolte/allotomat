using MediatR;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.Organizations
{
    public class GetProjectsStats : IRequestHandler<GetProjectsStats.Input, GetProjectsStats.Payload>
    {
        private readonly AppDbContext db;
        private readonly IClock clock;

        public GetProjectsStats(AppDbContext db, IClock clock)
        {
            this.db = db;
            this.clock = clock;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            var results = new Dictionary<long, ProjectStatsGraphType>();

            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var beneficiaryCount = await db.Beneficiaries.Where(x => projectId == x.Organization.ProjectId).AsNoTracking().CountAsync();
            var unspentLoyaltyFund = await db.Funds.Where(x => projectId == x.Card.ProjectId && x.ProductGroup.Name == ProductGroupType.LOYALTY).AsNoTracking().SumAsync(x => x.Amount);
            var subscriptions = await db.Subscriptions.Include(x => x.BudgetAllowances).Where(x => projectId == x.ProjectId).AsNoTracking().ToListAsync();
            var totalActiveSubscriptionsEnvelopes = subscriptions.Where(x => (x.FundsExpirationDate >= today || !x.IsFundsAccumulable)).Sum(x => x.BudgetAllowances.Sum(y => y.OriginalFund));

            return new Payload()
            {
                BeneficiaryCount = beneficiaryCount,
                UnspentLoyaltyFund = unspentLoyaltyFund,
                TotalActiveSubscriptionsEnvelopes = totalActiveSubscriptionsEnvelopes
            };
        }

        public decimal GetCardExpiredAmounts(List<ExpireFundTransaction> transactions)
        {
            return transactions.Sum(x => x.Amount);
        }

        public class Input : HaveProjectId, IRequest<Payload>
        {
        }

        public class Payload
        {
            public long BeneficiaryCount { get; set; }
            public decimal UnspentLoyaltyFund { get; set; }
            public decimal TotalActiveSubscriptionsEnvelopes { get; set; }
        }
    }
}
