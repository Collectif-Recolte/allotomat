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
        public static Pagination<SubscriptionEndReportGraphType> For(IEnumerable<SubscriptionEndReportGraphType> query, Page page)
        {
            var totalCount = query.Count();
            var itemPage = page.PageSize > 0
                ? query.Skip(page.Skip)
                : query.Where(x => false);

            return new Pagination<SubscriptionEndReportGraphType>(
                page: page,
                totalCount: totalCount,
                items: itemPage.Take(page.PageSize));
        }
    }
}
