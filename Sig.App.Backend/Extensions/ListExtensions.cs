using System;
using System.Collections.Generic;
using System.Linq;

namespace Sig.App.Backend.Extensions
{
    public static class ListExtensions
    {
        public static void UpdateNoKey<TItem, TSource>(this IList<TItem> target, IList<TSource> source, Action<TSource, TItem> mapSourceToTarget) where TItem : new()
        {
            while (target.Count > source.Count) target.RemoveAt(0);
            while (target.Count < source.Count) target.Add(new TItem());
            for (var i = 0; i < source.Count; i++)
                mapSourceToTarget(source[i], target[i]);
        }

        public static void AddRemoveUpdate<TTarget, TSource>(
            this IList<TTarget> targetList,
            IList<TSource> sourceList,
            Func<TTarget, TSource, bool> isSame,
            Action<TSource, TTarget> mapSourceToTarget,
            Action<TTarget> onRemoved = null,
            Action<TTarget> onAdded = null,
            Action<TTarget> onUpdated = null)
        where TTarget : new()
        {
            targetList.AddRemoveUpdate(
                sourceList,
                isSame,
                mapSourceToTarget,
                () => new TTarget(),
                onRemoved,
                onAdded,
                onUpdated);
        }

        public static void AddRemoveUpdate<TTarget, TSource>(
            this IList<TTarget> targetList,
            IList<TSource> sourceList,
            Func<TTarget, TSource, bool> isSame,
            Action<TSource, TTarget> mapSourceToTarget,
            Func<TTarget> factory,
            Action<TTarget> onRemoved = null,
            Action<TTarget> onAdded = null,
            Action<TTarget> onUpdated = null)
        {
            // Remove
            for (var i = targetList.Count - 1; i >= 0; i--)
            {
                var target = targetList[i];

                if (sourceList.Any(x => isSame(target, x)))
                    continue;

                targetList.RemoveAt(i);
                onRemoved?.Invoke(target);
            }

            foreach (var sourceItem in sourceList)
            {
                var existingItem = targetList.FirstOrDefault(x => isSame(x, sourceItem));

                if (existingItem != null)
                {
                    // Update
                    mapSourceToTarget(sourceItem, existingItem);
                    onUpdated?.Invoke(existingItem);
                }
                else
                {
                    // Add new
                    var newItem = factory();
                    mapSourceToTarget(sourceItem, newItem);

                    targetList.Add(newItem);
                    onAdded?.Invoke(newItem);
                }
            }
        }

        public static void AddRemove<TTarget, TSource>(
            this IList<TTarget> targetList,
            IList<TSource> sourceList,
            Func<TTarget, TSource, bool> isSame,
            Func<TSource, TTarget> convert,
            Action<TTarget> onRemoved = null,
            Action<TTarget> onAdded = null)
        {
            // Remove
            for (var i = targetList.Count - 1; i >= 0; i--)
            {
                var target = targetList[i];

                if (sourceList.Any(x => isSame(target, x)))
                    continue;

                targetList.RemoveAt(i);
                onRemoved?.Invoke(target);
            }

            // Add new
            foreach (var sourceItem in sourceList)
            {
                if (targetList.Any(x => isSame(x, sourceItem)))
                    continue;

                var newItem = convert(sourceItem);

                targetList.Add(newItem);
                onAdded?.Invoke(newItem);
            }
        }
    }
}
