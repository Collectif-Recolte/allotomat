using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.Gql.Bases
{
    public static class MarketAmountOwedPagination
    {
        public static async Task<MarketAmountOwedPagination<MarketAmountOwedGraphType>> For(IEnumerable<MarketAmountOwedGraphType> query, Page page)
        {
            var totalCount = query.Count();
            var itemPage = page.PageSize > 0
                ? query.Skip(page.Skip)
                : query.Where(x => false);

            return new MarketAmountOwedPagination<MarketAmountOwedGraphType>(
                page: page,
                totalCount: totalCount,
                items: itemPage.Take(page.PageSize));
        }

        public static MarketAmountOwedPagination<TResult> Map<TSource, TResult>(this MarketAmountOwedPagination<TSource> source, Func<TSource, TResult> map)
        {
            return new MarketAmountOwedPagination<TResult>(source.PageNumber, source.PageSize, source.TotalCount, source.Items.Select(map));
        }
    }

    public class MarketAmountOwedPagination<T> : Pagination<T>
    {
        public MarketAmountOwedPagination(Page page, long totalCount, IEnumerable<T> items)
            : this(page.PageNumber, page.PageSize, totalCount, items)
        {
        }

        public MarketAmountOwedPagination(int pageNumber, int pageSize, long totalCount, IEnumerable<T> items) : base(pageNumber, pageSize, totalCount, items)
        {
        }
    }
}
