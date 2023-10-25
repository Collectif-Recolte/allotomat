using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Sig.App.Backend.Utilities
{
    public static class Pagination
    {
        public static async Task<Pagination<T>> For<T>(IOrderedQueryable<T> query, Page page)
        {
            var totalCount = await query.CountAsync();
            var itemPage = page.PageSize > 0
                ? query.Skip(page.Skip)
                : query.Where(x => false);

            return new Pagination<T>(
                pageNumber: page.PageNumber,
                pageSize: page.PageSize,
                totalCount: totalCount,
                items: await itemPage.Take(page.PageSize).ToListAsync());
        }

        public static Task<Pagination<T>> For<T>(IOrderedQueryable<T> query, int page, int limit)
            => For(query, new Page(page, limit));


        public static Pagination<T> ForSync<T>(IOrderedQueryable<T> query, int page, int limit)
        {
            var result = For(query, new Page(page, limit));
            return result.Result;
        }

        public static Pagination<TResult> Map<TSource, TResult>(this Pagination<TSource> source, Func<TSource, TResult> map)
        {
            return new Pagination<TResult>(source.PageNumber, source.PageSize, source.TotalCount, source.Items.Select(map));
        }
    }

    public class Pagination<T>
    {
        public int PageNumber { get; }
        public int PageSize { get; }
        public long TotalCount { get; }
        public long TotalPages => TotalCount / PageSize + (TotalCount % PageSize == 0 ? 0 : 1);

        public IEnumerable<T> Items { get; }

        public Pagination(Page page, long totalCount, IEnumerable<T> items)
            : this(page.PageNumber, page.PageSize, totalCount, items)
        {
        }

        public Pagination(int pageNumber, int pageSize, long totalCount, IEnumerable<T> items)
        {
            if (pageSize < 0) throw new ArgumentException("Page size must be greater or equal to 0.", nameof(pageSize));

            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            Items = items;
        }
    }
}
