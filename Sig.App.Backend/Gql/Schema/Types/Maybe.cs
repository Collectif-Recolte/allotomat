using GraphQL.Conventions;
using System;

namespace Sig.App.Backend.Gql.Schema.Types
{
    [InputType]
    public class Maybe<T>
    {
        public Maybe() { }
        public Maybe(T value) => Value = value;

        public T Value { get; set; }

        public static implicit operator Maybe<T>(T val) => new Maybe<T>(val);
    }
}