using System;
using System.Reflection;
using GraphQL.Conventions;
using Microsoft.Extensions.DependencyInjection;

namespace Sig.App.Backend.Plugins.GraphQL
{
    /// <summary>
    /// Le ScopedDependencyInjector utilise un scope de services pour créer les services qui lui sont demandés.
    /// </summary>
    public class ScopedDependencyInjector : IDependencyInjector, IDisposable
    {
        private readonly IServiceScope scope;

        public ScopedDependencyInjector(IServiceProvider parentServices)
        {
            scope = parentServices.CreateScope();
        }

        public object Resolve(TypeInfo typeInfo)
        {
            return scope.ServiceProvider.GetService(typeInfo);
        }

        public void Dispose()
        {
            scope.Dispose();
        }
    }
}