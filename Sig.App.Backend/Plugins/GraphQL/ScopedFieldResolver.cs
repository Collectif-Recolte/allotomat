using GraphQL;
using GraphQL.Conventions;
using GraphQL.Conventions.Adapters;
using GraphQL.Conventions.Execution;
using GraphQL.Conventions.Types.Descriptors;

namespace Sig.App.Backend.Plugins.GraphQL
{
    /// <summary>
    /// Le ScopedFieldResolver crée un ScopedDependencyInjector pour chaque champ GraphQL qui doit être résolu.
    /// </summary>
    public class ScopedFieldResolver : FieldResolver
    {
        private readonly GraphFieldInfo fieldInfo;

        public ScopedFieldResolver(GraphFieldInfo fieldInfo) : base(fieldInfo)
        {
            this.fieldInfo = fieldInfo;
        }

        public override object Resolve(IResolveFieldContext context)
        {
            if (!fieldInfo.IsMethod) return base.Resolve(context);
            if (context.GetDependencyInjector() is not DependencyInjector originalInjector) return base.Resolve(context);

            var scopeFactory = originalInjector.Resolve<ScopedDependencyInjectorFactory>();
            
            try
            {
                context.SetDependencyInjector(scopeFactory.CreateScopedInjector());

                return base.Resolve(context);
            }
            finally
            {
                context.SetDependencyInjector(originalInjector);
            }
        }
    }
}