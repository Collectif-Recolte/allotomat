using DocumentFormat.OpenXml.Spreadsheet;
using GraphQL;
using GraphQL.Conventions;
using GraphQL.Conventions.Adapters;
using GraphQL.Conventions.Execution;
using GraphQL.Conventions.Types.Descriptors;
using GraphQL.Resolvers;
using System.Threading.Tasks;

namespace Sig.App.Backend.Plugins.GraphQL;

/// <summary>
/// Le ScopedFieldResolver crée un ScopedDependencyInjector pour chaque champ GraphQL qui doit être résolu.
/// </summary>
public class ScopedFieldResolver : IFieldResolver
{
    private readonly GraphFieldInfo fieldInfo;
    private readonly FieldResolver innerResolver;

    public ScopedFieldResolver(GraphFieldInfo fieldInfo)
    {
        this.fieldInfo = fieldInfo;

        innerResolver = new FieldResolver(fieldInfo);
    }

    public async ValueTask<object> ResolveAsync(IResolveFieldContext context)
    {
        if (!fieldInfo.IsMethod) return await innerResolver.ResolveAsync(context);
        if (context.GetDependencyInjector() is not DependencyInjector originalInjector) return await innerResolver.ResolveAsync(context);

        var scopeFactory = originalInjector.Resolve<ScopedDependencyInjectorFactory>();

        try
        {
            context.SetDependencyInjector(scopeFactory.CreateScopedInjector());

            return await innerResolver.ResolveAsync(context);
        }
        finally
        {
            context.SetDependencyInjector(originalInjector);
        }
    }
}