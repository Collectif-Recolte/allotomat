using GraphQL.Conventions;
using System;

namespace Sig.App.Backend.Extensions
{
    public static class GqlExtensions
    {
        public static long LongIdentifierForType<T>(this Id id)
        {
            var rawId = id.IdentifierForType<T>();

            if (!long.TryParse(rawId, out var longId))
                throw new ArgumentException($"Expected valid 64-bit number but got {rawId}");

            return longId;
        }

        public static string StringIdentifierForType<T>(this Id id)
        {
            var rawId = id.IdentifierForType<T>();

            if (string.IsNullOrWhiteSpace(rawId))
                throw new ArgumentException($"Expected valid string but got {rawId}");

            return rawId;
        }

        /// <param name="argumentName">Name of the argument carrying the scoped id, when it is neither
        /// "input" nor "id". Left null, the historical lookup order is unchanged.</param>
        public static object GetInputValue(this IResolutionContext ctx, string argumentName = null)
        {
            // The named argument is looked up first, then the historical fallbacks, so the callers
            // that pass no name keep resolving exactly what they resolved before.
            var input = argumentName != null ? ctx.GetArgument(argumentName) : null;

            input ??= ctx.GetArgument("input");
            input ??= ctx.GetArgument("id");

            if (input == null)
                return null;

            var inputType = input.GetType();
            if (inputType.IsGenericType && inputType.GetGenericTypeDefinition() == typeof(NonNull<>))
            {
                input = inputType.GetProperty("Value").GetValue(input);
            }

            return input;
        }
    }
}
