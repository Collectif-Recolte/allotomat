using GraphQL.Conventions;
using System;
using System.Threading.Tasks;

namespace Sig.App.Backend.Gql
{
    public abstract class LazyGraphType<TData>
    {
        private Lazy<Task<TData>> LazyData { get; }
        protected Task<TData> Data => LazyData.Value;

        protected async Task<TResult> WithData<TResult>(Func<TData, TResult> action)
        {
            return action(await Data);
        }
        protected async Task<NonNull<TResult>> WithDataNonNull<TResult>(Func<TData, TResult> action) where TResult : class
        {
            return action(await Data);
        }

        protected LazyGraphType(TData data)
        {
            LazyData = new Lazy<Task<TData>>(Task.FromResult(data));
        }

        protected LazyGraphType(Func<Task<TData>> loadData)
        {
            LazyData = new Lazy<Task<TData>>(loadData);
        }
    }
}