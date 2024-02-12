using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public static class PaymentConflictPagination
    {
        public static async Task<PaymentConflictPagination<T>> For<T>(IOrderedQueryable<T> query, Page page, long conflictPaymentCount)
        {
            var totalCount = await query.CountAsync();
            var itemPage = page.PageSize > 0
                ? query.Skip(page.Skip)
                : query.Where(x => false);

            return new PaymentConflictPagination<T>(
                pageNumber: page.PageNumber,
                pageSize: page.PageSize,
                totalCount: totalCount,
                items: await itemPage.Take(page.PageSize).ToListAsync(),
                conflictPaymentCount);
        }

        public static PaymentConflictPagination<TResult> Map<TSource, TResult>(this PaymentConflictPagination<TSource> source, Func<TSource, TResult> map)
        {
            return new PaymentConflictPagination<TResult>(source.PageNumber, source.PageSize, source.TotalCount, source.Items.Select(map), source.ConflictPaymentCount);
        }
    }

    public class PaymentConflictPagination<T> : Pagination<T>
    {
        public long ConflictPaymentCount { get; set; }

        public PaymentConflictPagination(int pageNumber, int pageSize, long totalCount, IEnumerable<T> items, long conflictPaymentCount) : base(pageNumber, pageSize, totalCount, items)
        {
            ConflictPaymentCount = conflictPaymentCount;
        }
    }
}
