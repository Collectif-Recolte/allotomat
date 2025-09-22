using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sig.App.Backend.Gql.Bases
{
    public static class SubscriptionEndReportPagination
    {
        public static SubscriptionEndReportPagination<SubscriptionEndReportGraphType> For(IEnumerable<SubscriptionEndReportGraphType> query, Page page)
        {
            var totalCount = query.Count();
            var itemPage = page.PageSize > 0
                ? query.Skip(page.Skip)
                : query.Where(x => false);

            return new SubscriptionEndReportPagination<SubscriptionEndReportGraphType>(
                page: page,
                totalCount: totalCount,
                items: itemPage.Take(page.PageSize));
        }
    }

    public class SubscriptionEndReportPagination<T> : Pagination<T>
    {
        public SubscriptionEndReportTotalGraphType Total { get; set; }

        public SubscriptionEndReportPagination(Page page, long totalCount, IEnumerable<T> items)
            : this(page.PageNumber, page.PageSize, totalCount, items)
        {
        }

        public SubscriptionEndReportPagination(int pageNumber, int pageSize, long totalCount, IEnumerable<T> items) : base(pageNumber, pageSize, totalCount, items)
        {
        }
    }
}
