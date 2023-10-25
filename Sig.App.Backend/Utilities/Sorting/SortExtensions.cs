using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Sig.App.Backend.Utilities.Sorting
{
    public static class SortExtensions
    {
        public static IOrderedQueryable<T> SortBy<T, TField>(this IQueryable<T> query, IEnumerable<Sort<TField>> sort, Func<TField, Expression<Func<T, object>>> getSortExpr) where TField : struct
        {
            IOrderedQueryable<T> sorted = null;

            foreach (var s in sort)
            {
                var sortExpr = getSortExpr(s.Field);

                sorted = sorted == null 
                    ? query.SortBy(sortExpr, s.Order) 
                    : sorted.ThenSortBy(sortExpr, s.Order);
            }

            return sorted;
        }
        
        public static IOrderedQueryable<T> SortBy<T, TKey>(this IQueryable<T> query, Expression<Func<T, TKey>> prop, SortOrder order)
        {
            switch (order)
            {
                case SortOrder.Asc: return query.OrderBy(prop);
                case SortOrder.Desc: return query.OrderByDescending(prop);
                default: throw new ArgumentOutOfRangeException(nameof(order), order, null);
            }
        }

        public static IOrderedQueryable<T> ThenSortBy<T, TKey>(this IOrderedQueryable<T> query, Expression<Func<T, TKey>> prop, SortOrder order)
        {
            switch (order)
            {
                case SortOrder.Asc: return query.ThenBy(prop);
                case SortOrder.Desc: return query.ThenByDescending(prop);
                default: throw new ArgumentOutOfRangeException(nameof(order), order, null);
            }
        }

        public static IOrderedEnumerable<T> SortBy<T, TKey>(this IEnumerable<T> results, Func<T, TKey> prop, SortOrder order)
        {
            switch (order)
            {
                case SortOrder.Asc: return results.OrderBy(prop);
                case SortOrder.Desc: return results.OrderByDescending(prop);
                default: throw new ArgumentOutOfRangeException(nameof(order), order, null);
            }
        }

        public static IOrderedEnumerable<T> ThenSortBy<T, TKey>(this IOrderedEnumerable<T> results, Func<T, TKey> prop, SortOrder order)
        {
            switch (order)
            {
                case SortOrder.Asc: return results.ThenBy(prop);
                case SortOrder.Desc: return results.ThenByDescending(prop);
                default: throw new ArgumentOutOfRangeException(nameof(order), order, null);
            }
        }

        public static SortOrder Invert(this SortOrder order)
        {
            switch (order)
            {
                case SortOrder.Asc: return SortOrder.Desc;
                case SortOrder.Desc: return SortOrder.Asc;
                default: throw new ArgumentOutOfRangeException(nameof(order), order, null);
            }
        }
    }
}