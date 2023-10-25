using System.Diagnostics;
using System.Reflection;
using GraphQL.Conventions;
using GraphQL.Conventions.Adapters;
using GraphQL.NodaTime;
using NodaTime;

namespace Sig.App.Backend.Plugins.GraphQL
{
    public static class GraphQLEngineFactory
    {
        public static GraphQLEngine Create()
        {
            var engine = GraphQLEngine.New();

            InjectScopedFieldResolverFactory(engine);
            
            engine.RegisterScalarType<LocalDate, LocalDateGraphType>()
                .RegisterScalarType<OffsetDateTime, RfcOffsetDateTimeGraphType>()
                .WithQueryAndMutation<Gql.Schema.Query, Gql.Schema.Mutation>()
                .BuildSchema();

            return engine;
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

            var graphTypeAdapter = (GraphTypeAdapter)graphTypeAdapterField.GetValue(engine);
            Debug.Assert(graphTypeAdapter != null, nameof(graphTypeAdapter) + " != null");

            graphTypeAdapter.FieldResolverFactory = fieldInfo => new ScopedFieldResolver(fieldInfo);
        }
    }
}
