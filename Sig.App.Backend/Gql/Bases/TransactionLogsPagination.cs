using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.Gql.Bases
{
    public static class TransactionLogsPagination
    {
        public static async Task<TransactionLogsPagination<TransactionLog>> For(IOrderedQueryable<TransactionLog> query, Page page)
        {
            var totalCount = await query.CountAsync();
            var itemPage = page.PageSize > 0
                ? query.Skip(page.Skip)
                : query.Where(x => false);

            return new TransactionLogsPagination<TransactionLog>(
                page: page,
                totalCount: totalCount,
                items: await itemPage.Take(page.PageSize).ToListAsync(),
                amountDueToMarkets: GetTransactionTotal(itemPage));
        }

        public static TransactionLogsPagination<TResult> Map<TSource, TResult>(this TransactionLogsPagination<TSource> source, Func<TSource, TResult> map)
        {
            return new TransactionLogsPagination<TResult>(source.PageNumber, source.PageSize, source.TotalCount, source.Items.Select(map), source.AmountDueToMarkets);
        }

        private static decimal GetTransactionTotal(IQueryable<TransactionLog> itemPage)
        {
            return itemPage.Sum(x => x.Discriminator == TransactionLogDiscriminator.PaymentTransactionLog ? x.TotalAmount : x.Discriminator == TransactionLogDiscriminator.RefundPaymentTransactionLog ? -x.TotalAmount : 0);
        }
    }

    public class TransactionLogsPagination<T> : Pagination<T>
    {
        public decimal AmountDueToMarkets { get; }

        public TransactionLogsPagination(Page page, long totalCount, IEnumerable<T> items, decimal amountDueToMarkets)
            : this(page.PageNumber, page.PageSize, totalCount, items, amountDueToMarkets)
        {
        }

        public TransactionLogsPagination(int pageNumber, int pageSize, long totalCount, IEnumerable<T> items, decimal amountDueToMarkets) : base(pageNumber, pageSize, totalCount, items)
        {
            AmountDueToMarkets = amountDueToMarkets;
        }
    }
}
