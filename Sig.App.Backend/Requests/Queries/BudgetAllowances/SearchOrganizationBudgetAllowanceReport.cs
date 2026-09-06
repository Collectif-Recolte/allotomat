using MediatR;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.BudgetAllowanceLogs;
using Sig.App.Backend.Utilities;
using Sig.App.Backend.Utilities.Sorting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.Organizations
{
    public class SearchOrganizationBudgetAllowanceReport : IRequestHandler<SearchOrganizationBudgetAllowanceReport.Query, Pagination<BudgetAllowanceLog>>
    {
        private readonly AppDbContext db;

        public SearchOrganizationBudgetAllowanceReport(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<Pagination<BudgetAllowanceLog>> Handle(Query request, CancellationToken cancellationToken)
        {
            // Le groupe voit les ajustements de ses enveloppes, mais aussi les transferts reçus : sinon son
            // total changerait sans trace, ce que le rapport cherche justement à expliquer.
            IQueryable<BudgetAllowanceLog> query = db.BudgetAllowanceLogs
                .Where(x => (x.OrganizationId == request.OrganizationId || x.TargetOrganizationId == request.OrganizationId)
                    && x.CreatedAtUtc >= request.StartDate && x.CreatedAtUtc <= request.EndDate);

            if (request.Subscriptions != null && request.Subscriptions.Count() > 0)
            {
                query = query.Where(x =>
                    (x.SubscriptionId.HasValue && request.Subscriptions.Contains(x.SubscriptionId.Value))
                    || (x.TargetSubscriptionId.HasValue && request.Subscriptions.Contains(x.TargetSubscriptionId.Value)));
            }

            var result = await Pagination.For(query.SortBy(x => x.CreatedAtUtc, SortOrder.Desc), request.Page);

            return result;
        }

        public class Query : IRequest<Pagination<BudgetAllowanceLog>>
        {
            public Page Page { get; set; }
            public long OrganizationId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public IEnumerable<long> Subscriptions { get; set; }
        }
    }
}
