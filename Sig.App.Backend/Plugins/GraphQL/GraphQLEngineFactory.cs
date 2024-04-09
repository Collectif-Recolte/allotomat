using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using GraphQL.Conventions;
using GraphQL.Conventions.Adapters;
using GraphQL.NodaTime;
using GraphQL.Utilities.Federation;
using NodaTime;

namespace Sig.App.Backend.Plugins.GraphQL;

/// <summary>
/// Marque une classe comme un conteneur d'extensions GraphQL.
/// Les classes avec cet attribut seront automatiquement découvertes au démarrage de l'application.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SchemaExtensionAttribute : Attribute { }

// Points d'entrée du schéma. Étendues par les modules.
[Name("Query")] public class GqlQuery { }
[Name("Mutation")] public class GqlMutation { }

public static class GraphQLEngineFactory
{
    public static GraphQLEngine Create()
    {
        var engine = GraphQLEngine.New();

        InjectScopedFieldResolverFactory(engine);

        RegisterScalars(engine);

        engine.WithQueryAndMutation<GqlQuery, GqlMutation>();
        RegisterModules(engine);

        engine.BuildSchema();

        return engine;
    }

    private static void RegisterScalars(GraphQLEngine engine)
    {
        engine
            .RegisterScalarType<LocalDate, LocalDateGraphType>()
            .RegisterScalarType<OffsetDateTime, RfcOffsetDateTimeGraphType>();
    }

    private static void RegisterModules(GraphQLEngine engine)
    {
        // Découvre toutes les classes marquées de l'attribut [SchemaExtension]
        var allTypes = typeof(GraphQLEngineFactory).Assembly.GetTypes();
        var extensionTypes = allTypes.Where(t => t.GetCustomAttribute<SchemaExtensionAttribute>() != null);

        // Et les ajoute au schéma
        foreach (var type in extensionTypes)
        {
            engine.WithQueryExtensions(type);
        }
    }

    /// <summary>
    /// GraphQL.Conventions n'offre pas d'API pour configurer un FieldResolverFactory custom. Le système est tout de
    /// même pensé pour être extensible, puisque l'API propose une méthode WithFieldResolutionStrategy, mais
    /// celle-ci ne supporte que les implémentations fournies par la librairie elle-même.
    /// 
    /// On utilise donc la réflexion pour aller remplacer l'implémentation par défaut.
    ///
    /// Cette implémentation crée des ScopedFieldResolver, qui dérive du FieldResolver de la librairie pour ajouter
    /// la gestion d'un scope de services unique à chaque resolver GraphQL afin d'avoir notamment une instance du
    /// DbContext unique à chaque resolver, ce qui évite les erreurs de concurrence lors de l'évaluation des champs
    /// d'une requête en parallèle.
    /// </summary>
    /// <param name="engine"></param>
    private static void InjectScopedFieldResolverFactory(GraphQLEngine engine)
    {
        var graphTypeAdapterField = engine.GetType().GetField("_graphTypeAdapter", BindingFlags.Instance | BindingFlags.NonPublic);
        Debug.Assert(graphTypeAdapterField != null, nameof(graphTypeAdapterField) + " != null");

        var graphTypeAdapter = graphTypeAdapterField.GetValue(engine) as GraphTypeAdapter;
        Debug.Assert(graphTypeAdapter != null, nameof(graphTypeAdapter) + " != null");

        graphTypeAdapter.FieldResolverFactory = fieldInfo => new ScopedFieldResolver(fieldInfo);
    }
}