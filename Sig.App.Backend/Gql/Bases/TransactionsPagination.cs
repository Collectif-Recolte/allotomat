using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.Gql.Bases
{
    public static class TransactionsPagination
    {
        public static async Task<TransactionsPagination<Transaction>> For(IOrderedQueryable<Transaction> query, Page page)
        {
            var totalCount = await query.CountAsync();
            var itemPage = page.PageSize > 0
                ? query.Skip(page.Skip)
                : query.Where(x => false);

            return new TransactionsPagination<Transaction>(
                page: page,
                totalCount: totalCount,
                items: await itemPage.Take(page.PageSize).ToListAsync());
        }

        public static TransactionsPagination<TResult> Map<TSource, TResult>(this TransactionsPagination<TSource> source, Func<TSource, TResult> map)
        {
            return new TransactionsPagination<TResult>(source.PageNumber, source.PageSize, source.TotalCount, source.Items.Select(map));
        }
    }

    public class TransactionsPagination<T> : Pagination<T>
    {
        public TransactionsPagination(Page page, long totalCount, IEnumerable<T> items)
            : this(page.PageNumber, page.PageSize, totalCount, items)
        {
        }

        public TransactionsPagination(int pageNumber, int pageSize, long totalCount, IEnumerable<T> items) : base(pageNumber, pageSize, totalCount, items)
        {
        }
    }
}
