using System;
using System.Collections.Generic;

namespace Sig.App.Backend.Plugins.GraphQL
{
    /// <summary>
    /// Cette classe est responsable de créer des instances de ScopedDependencyInjector et de les Disposer à la fin du
    /// scope original (qui devrait correspondre à une requête HTTP)
    /// </summary>
    public class ScopedDependencyInjectorFactory : IDisposable
    {
        private readonly IServiceProvider services;
        private readonly List<ScopedDependencyInjector> scopedInjectors = new();

        public ScopedDependencyInjectorFactory(IServiceProvider services)
        {
            this.services = services;
        }

        public ScopedDependencyInjector CreateScopedInjector()
        {
            var scopedInjector = new ScopedDependencyInjector(services);
            scopedInjectors.Add(scopedInjector);
            return scopedInjector;
        }

        public void Dispose()
        {
            scopedInjectors.ForEach(x => x.Dispose());
        }
    }
}