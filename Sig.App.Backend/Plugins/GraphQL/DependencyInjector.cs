using System;
using System.Reflection;
using GraphQL.Conventions;

namespace Sig.App.Backend.Plugins.GraphQL
{
    public class DependencyInjector : IDependencyInjector
    {
        private readonly IServiceProvider services;

        public DependencyInjector(IServiceProvider services)
        {
            this.services = services;
        }

        public object Resolve(TypeInfo typeInfo)
        {
            return services.GetService(typeInfo);
        }
    }
}