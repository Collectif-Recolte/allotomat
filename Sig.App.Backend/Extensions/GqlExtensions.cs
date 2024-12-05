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

        public static object GetInputValue(this IResolutionContext ctx)
        {
            var input = ctx.GetArgument("input");
            if (input == null)
            {
                var id = ctx.GetArgument("id");
                if (id == null)
                    return null;
                else
                    input = id;
            }

            var inputType = input.GetType();
            if (inputType.IsGenericType && inputType.GetGenericTypeDefinition() == typeof(NonNull<>))
            {
                input = inputType.GetProperty("Value").GetValue(input);
            }

            return input;
        }
    }
}
