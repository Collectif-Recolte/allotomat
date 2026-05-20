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
        public static Task<MarketAmountOwedPagination<MarketAmountOwedGraphType>> For(IEnumerable<MarketAmountOwedGraphType> query, Page page)
        {
            var allItems = query.ToList();
            var totalAmount = allItems.Sum(x => x.Amount);
            var totalCount = allItems.Count;
            var itemPage = page.PageSize > 0
                ? allItems.Skip(page.Skip)
                : allItems.Where(x => false);

            return Task.FromResult(new MarketAmountOwedPagination<MarketAmountOwedGraphType>(
                page: page,
                totalCount: totalCount,
                items: itemPage.Take(page.PageSize),
                totalAmount: totalAmount));
        }

        public static MarketAmountOwedPagination<TResult> Map<TSource, TResult>(this MarketAmountOwedPagination<TSource> source, Func<TSource, TResult> map)
        {
            return new MarketAmountOwedPagination<TResult>(source.PageNumber, source.PageSize, source.TotalCount, source.Items.Select(map), source.TotalAmount);
        }
    }

    public class MarketAmountOwedPagination<T> : Pagination<T>
    {
        public decimal TotalAmount { get; }

        public MarketAmountOwedPagination(Page page, long totalCount, IEnumerable<T> items, decimal totalAmount)
            : this(page.PageNumber, page.PageSize, totalCount, items, totalAmount)
        {
        }

        public MarketAmountOwedPagination(int pageNumber, int pageSize, long totalCount, IEnumerable<T> items, decimal totalAmount) : base(pageNumber, pageSize, totalCount, items)
        {
            TotalAmount = totalAmount;
        }
    }
}
