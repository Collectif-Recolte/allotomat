using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GraphQL;
using GraphQL.Conventions;
using GraphQL.Conventions.Execution;
using GraphQL.Validation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using Sig.App.Backend.Authorization;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Requests.Queries.Users;
using Sig.App.Backend.Services.Permission;
using Sig.App.Backend.Utilities;

namespace Sig.App.BackendTests.Authorization
{
    // [RequirePermission] est un filtre d'exécution GraphQL.Conventions: il ne s'exécute que dans le
    // pipeline de résolution du schéma, jamais dans le handler. La seule façon de prouver son
    // comportement est donc de router une vraie requête à travers le moteur GraphQL réel, pas d'appeler
    // le handler MediatR directement.
    public class RequirePermissionAttributeTest : TestBase
    {
        private const string UsersQuery = @"
            query {
              users(page: 1, limit: 10, searchText: """") {
                items { id }
              }
            }";

        // FILETS-32: hasPermission est un champ d'instance de RequirePermissionAttribute, or le moteur
        // GraphQL est enregistré en singleton de processus (Startup.cs) et ses instances d'attribut sont
        // construites une seule fois avec le schéma. Un appelant autorisé qui franchit la garde laisse
        // donc hasPermission à vrai pour cette instance, et tout appelant suivant sur le même moteur est
        // accepté sans égard à sa propre permission, jusqu'au redémarrage du processus.
        //
        // Ce test partage délibérément un seul GraphQLEngine entre les deux appels: BuildEngine() n'est
        // invoqué qu'une fois, et la même instance sert aux deux exécutions. En reconstruire une par appel
        // recréerait l'instance d'attribut à chaque fois et effacerait l'état fautif sans rien prouver.
        [Fact]
        public async Task Execute_SecondCallerWithoutPermission_IsRefusedEvenAfterFirstAuthorizedCallOnSameEngine()
        {
            var admin = AddUser("admin@example.com", UserType.PCAAdmin);
            var merchant = AddUser("merchant@example.com", UserType.Merchant);
            SetupMediatorForUsers();

            var sharedEngine = BuildEngine();

            var authorizedResult = await ExecuteAsync(sharedEngine, UsersQuery, admin);
            AssertSucceeded(authorizedResult);

            var unauthorizedResult = await ExecuteAsync(sharedEngine, UsersQuery, merchant);
            AssertRefused(unauthorizedResult);
        }

        // FILETS-38: aucun champ d'instance de la classe ne porte d'état propre à un appel. L'attribut
        // est construit une seule fois avec le schéma (moteur GraphQL en singleton de processus), donc
        // tout champ d'instance non readonly serait relu après un await par une requête concurrente et
        // porterait l'état (service, contexte EF) d'un appel qui n'est pas le sien. Contrairement à la
        // course ci-dessus, ce défaut n'exige pas d'entrelacement réel: un champ non readonly suffit à
        // le constituer, donc une inspection structurelle par réflexion le détecte de façon fiable.
        [Fact]
        public void RequirePermissionAttribute_HasNoInstanceFieldsCarryingPerCallState()
        {
            var mutableInstanceFields = typeof(RequirePermissionAttribute)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(f => !f.IsInitOnly)
                .Select(f => f.Name);

            mutableInstanceFields.Should().BeEmpty();
        }

        private void SetupMediatorForUsers()
        {
            MediatorMock
                .Setup(x => x.Send(It.IsAny<SearchUsers.Query>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Pagination<AppUser>(1, 10, 0, Enumerable.Empty<AppUser>()));
        }

        private async Task<ExecutionResult> ExecuteAsync(GraphQLEngine engine, string query, AppUser user)
        {
            SetLoggedInUser(user);

            var services = new ServiceCollection();
            services.AddSingleton(DbContext);
            services.AddSingleton(UserManager);
            services.AddSingleton(HttpContextAccessor);
            services.AddSingleton(Mediator);
            services.AddTransient<PermissionService>();
            var serviceProvider = services.BuildServiceProvider();

            var injector = new FlatDependencyInjector(serviceProvider);
            var userContext = new AppUserContext(serviceProvider, null);

            var executor = engine
                .NewExecutor()
                .WithValidationRules(Array.Empty<IValidationRule>())
                .WithUserContext(userContext)
                .WithDependencyInjector(injector)
                .WithQueryString(query);

            return await executor.ExecuteAsync();
        }

        private static void AssertRefused(ExecutionResult result)
        {
            result.Errors.Should().NotBeNullOrEmpty();
            result.Errors.Select(UnwrapException).Should().Contain(ex => ex is UnauthorizedAccessException);
        }

        private static void AssertSucceeded(ExecutionResult result)
        {
            if (result.Errors == null || !result.Errors.Any()) return;

            var unwrapped = result.Errors.Select(UnwrapException).ToList();
            unwrapped.Should().NotContain(ex => ex is UnauthorizedAccessException);
        }

        private static Exception UnwrapException(ExecutionError error)
        {
            var exception = error.InnerException;
            return exception is FieldResolutionException ? exception.InnerException : exception;
        }

        private static GraphQLEngine BuildEngine()
        {
            // GraphQLEngineFactory.Create() reflète les classes [ApplyPolicy]/[AnnotatePolicy] du schéma
            // au moment du build, ce qui exige un IAuthorizationPolicyProvider déjà en place (voir
            // Startup.cs, qui fait la même chose avant d'appeler GraphQLEngineFactory.Create()).
            var authServices = new ServiceCollection();
            authServices.AddLogging();
            authServices.AddAuthorization();
            var authProvider = authServices.BuildServiceProvider().GetRequiredService<IAuthorizationPolicyProvider>();
            AnnotatePolicyAttribute.AuthorizationPolicyProvider = authProvider;

            return GraphQLEngineFactory.Create();
        }

        // IDependencyInjector minimal résolvant directement dans un IServiceProvider à plat. Volontairement
        // distinct de Sig.App.Backend.Plugins.GraphQL.DependencyInjector: ScopedFieldResolver ne crée un
        // scope par champ que pour ce type précis, et la requête testée ici n'a besoin d'aucune isolation
        // de scope entre champs.
        private sealed class FlatDependencyInjector : IDependencyInjector
        {
            private readonly IServiceProvider services;

            public FlatDependencyInjector(IServiceProvider services)
            {
                this.services = services;
            }

            public object Resolve(System.Reflection.TypeInfo typeInfo) => services.GetService(typeInfo);
        }
    }
}
