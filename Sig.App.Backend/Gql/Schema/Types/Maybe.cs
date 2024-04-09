#nullable enable
using GraphQL.Conventions;
using Sig.App.Backend.Plugins.GraphQL;

namespace Sig.App.Backend.Gql.Schema.Types
{
    [InputType, DistinctNonNullType]
    public class Maybe<T>
    {
        public Maybe() { }
        public Maybe(T value) => Value = value;

        public T? Value { get; set; }

        public static implicit operator Maybe<T>(T val) => new(val);
    }
}
