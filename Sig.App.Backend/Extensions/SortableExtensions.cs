using System.Collections.Generic;
using System.Linq;
using Sig.App.Backend.DbModel;

namespace Sig.App.Backend.Extensions;

public static class SortableExtensions
{
    public static IOrderedEnumerable<T> Sorted<T>(this IEnumerable<T> items) where T : ISortable 
        => items.OrderBy(x => x.SortOrder);

    public static IOrderedQueryable<T> Sorted<T>(this IQueryable<T> items) where T : ISortable 
        => items.OrderBy(x => x.SortOrder);
}