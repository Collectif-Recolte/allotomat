using GraphQL.Conventions;
using System;
using Sig.App.Backend.Gql.Schema.Types;

namespace Sig.App.Backend.Extensions
{
    public static class MaybeExtensions
    {
        public const string NONE = "__NONE__";

        public static bool IsSet<T>(this Maybe<T> maybe) => maybe != null;

        public static void IfSet<T>(this Maybe<T> maybe, Action<T> action)
        {
            if (maybe.IsSet())
                action(maybe.Value);
        }

        public static TResult IfSet<T, TResult>(this Maybe<T> maybe, Func<T, TResult> action, TResult ifNotSet = default(TResult))
        {
            return maybe.IsSet() ? action(maybe.Value) : ifNotSet;
        }

        public static Maybe<TResult> Map<T, TResult>(this Maybe<T> maybe, Func<T, TResult> action)
        {
            if (maybe == null) return null;
            return action(maybe.Value);
        }

        public static string Stringify<T>(this Maybe<T> maybe) => maybe.IfSet(x => x.ToString(), NONE);
    }

    public static class NonNullExtensions
    {
        public static NonNull<T> NonNull<T>(this T val) where T : class => (NonNull<T>)val;
        public static string Trim(this NonNull<string> val) => val.Value.Trim();
    }
}