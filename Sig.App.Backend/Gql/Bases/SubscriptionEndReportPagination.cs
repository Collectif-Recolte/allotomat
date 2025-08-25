using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public static SubscriptionEndReportPagination<TResult> Map<TSource, TResult>(this SubscriptionEndReportPagination<TSource> source, Func<TSource, TResult> map)
        {
            return new SubscriptionEndReportPagination<TResult>(source.PageNumber, source.PageSize, source.TotalCount, source.Items.Select(map));
        }
    }

    public class SubscriptionEndReportPagination<T> : Pagination<T>
    {
        public SubscriptionEndReportPagination(Page page, long totalCount, IEnumerable<T> items)
            : this(page.PageNumber, page.PageSize, totalCount, items)
        {
        }

        public SubscriptionEndReportPagination(int pageNumber, int pageSize, long totalCount, IEnumerable<T> items) : base(pageNumber, pageSize, totalCount, items)
        {
        }
    }
}
