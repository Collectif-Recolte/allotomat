using MediatR;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.BudgetAllowanceLogs;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Utilities;
using Sig.App.Backend.Utilities.Sorting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.Projects
{
    public class SearchProjectBudgetAllowanceReport : IRequestHandler<SearchProjectBudgetAllowanceReport.Query, Pagination<BudgetAllowanceLog>>
    {
        private readonly AppDbContext db;

        public SearchProjectBudgetAllowanceReport(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<Pagination<BudgetAllowanceLog>> Handle(Query request, CancellationToken cancellationToken)
        {
            IQueryable<BudgetAllowanceLog> query = db.BudgetAllowanceLogs.Where(x => x.ProjectId == request.ProjectId && x.CreatedAtUtc >= request.StartDate && x.CreatedAtUtc <= request.EndDate);

            if (request.Subscriptions != null && request.Subscriptions.Count() > 0)
            {
                query = query.Where(x => request.Subscriptions.Contains(x.SubscriptionId.Value));
            }

            if (request.Organizations != null && request.Organizations.Count() > 0)
            {
                query = query.Where(x => request.Organizations.Contains(x.OrganizationId.Value));
            }

            var result = await Pagination.For(query.SortBy(x => x.CreatedAtUtc, SortOrder.Desc), request.Page);

            return result;
        }

        public class Query : IRequest<Pagination<BudgetAllowanceLog>>
        {
            public Page Page { get; set; }
            public long ProjectId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public IEnumerable<long> Subscriptions { get; set; }
            public IEnumerable<long> Organizations { get; set; }
        }
    }
}
