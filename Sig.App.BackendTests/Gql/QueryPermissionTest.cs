using System;
using System.Linq;
using System.Security.Claims;
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
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Requests.Commands.Queries.Transactions;
using Sig.App.Backend.Services.Permission;

namespace Sig.App.BackendTests.Gql
{
    // [RequirePermission] est un filtre d'exécution GraphQL.Conventions: il ne s'exécute que dans le
    // pipeline de résolution du schéma, jamais dans le handler. La seule façon de prouver qu'il bloque
    // un appel est donc de router une vraie requête à travers le moteur GraphQL réel, pas d'appeler le
    // handler MediatR directement.
    //
    // Le moteur est partagé entre les appels d'une même méthode de test, jamais reconstruit à chaque
    // exécution: FILETS-32 documente un champ d'instance de RequirePermissionAttribute qui n'est
    // jamais réinitialisé, ce qui n'existe que sur le moteur singleton que Startup.cs enregistre en
    // production. Reconstruire un moteur neuf par appel masquerait ce défaut et ne prouverait la garde
    // que dans une configuration que la production n'utilise jamais. Pour GenerateTransactionsReport,
    // qui passe par cet attribut, l'appel refusé est donc exécuté avant l'appel accepté sur le MÊME
    // moteur: un appel accepté fige ce champ à vrai pour tous les suivants, donc l'ordre inverse
    // masquerait un refus qui devrait avoir lieu.
    public class QueryPermissionTest : TestBase
    {
        private const string GenerateTransactionsReportQuery = @"
            query {
              generateTransactionsReport(
                projectId: ""cHJvamVjdDox""
                startDate: ""2025-01-01T00:00:00.000Z""
                endDate: ""2025-01-31T23:59:59.000Z""
                organizations: []
                subscriptions: []
                withoutSubscription: false
                categories: []
                markets: []
                marketGroups: []
                transactionTypes: []
                giftCardTransactionTypes: []
                searchText: """"
                timeZoneId: ""America/Toronto""
                language: FRENCH
              )
            }";

        // Id.New<Market>(1), soit "Market:1" encodé. GraphQL.Conventions encode le nom du type C#
        // avec sa casse d'origine (majuscule): un identifiant "market:1" en minuscule, comme en
        // portait la version précédente de ce test, ne correspond à aucun type et échoue au décodage
        // avant même d'atteindre la garde, ce qu'un ArgumentException aurait dû trahir.
        private const string GenerateTransactionsReportForMarketOwnMarketQuery = @"
            query {
              generateTransactionsReportForMarket(
                marketId: ""TWFya2V0OjE=""
                startDate: ""2025-01-01T00:00:00.000Z""
                endDate: ""2025-01-31T23:59:59.000Z""
                timeZoneId: ""America/Toronto""
                language: FRENCH
              )
            }";

        // Id.New<Market>(2), distinct du marché géré par le commerçant des tests ci-dessous
        private const string GenerateTransactionsReportForMarketOtherMarketQuery = @"
            query {
              generateTransactionsReportForMarket(
                marketId: ""TWFya2V0OjI=""
                startDate: ""2025-01-01T00:00:00.000Z""
                endDate: ""2025-01-31T23:59:59.000Z""
                timeZoneId: ""America/Toronto""
                language: FRENCH
              )
            }";

        // AUDIT-01/FILETS-30: GenerateTransactionsReport n'exposait aucune garde et n'importe quel
        // compte authentifié pouvait l'appeler. La garde exigée est GlobalPermission.ManageTransactions,
        // la même que l'écran TransactionLogs qu'il reproduit.
        [Fact]
        public async Task GenerateTransactionsReport_RequiresManageTransactions()
        {
            var merchant = AddUser("merchant@example.com", UserType.Merchant);
            var organizationManager = AddUser("om@example.com", UserType.OrganizationManager);
            SetupMediatorForReport();

            var engine = BuildEngine();

            var refused = await ExecuteAsync(engine, GenerateTransactionsReportQuery, merchant);
            AssertRefused(refused);

            var accepted = await ExecuteAsync(engine, GenerateTransactionsReportQuery, organizationManager);
            AssertSucceeded(accepted);
        }

        // FILETS-30: GenerateTransactionsReportForMarket n'exposait aucune garde. Un commerçant doit
        // pouvoir exporter le rapport de SON marché (bouton "Exporter un rapport" du commerçant).
        [Fact]
        public async Task GenerateTransactionsReportForMarket_MerchantIsAcceptedOnOwnMarket()
        {
            var merchant = AddUser("merchant@example.com", UserType.Merchant, claims: new[] { new Claim(AppClaimTypes.MarketManagerOf, "1") });

            SetupMediatorForMarketReport();

            var result = await ExecuteAsync(BuildEngine(), GenerateTransactionsReportForMarketOwnMarketQuery, merchant);

            AssertSucceeded(result);
        }

        // FILETS-30: sans garde, rien ne vérifiait que le marché demandé est bien celui du commerçant
        // appelant. Un commerçant gestionnaire du marché 1 ne doit pas pouvoir exporter le rapport du
        // marché 2.
        [Fact]
        public async Task GenerateTransactionsReportForMarket_MerchantIsRefusedOnOtherMarket()
        {
            var merchant = AddUser("merchant@example.com", UserType.Merchant, claims: new[] { new Claim(AppClaimTypes.MarketManagerOf, "1") });

            SetupMediatorForMarketReport();

            var result = await ExecuteAsync(BuildEngine(), GenerateTransactionsReportForMarketOtherMarketQuery, merchant);

            AssertRefused(result);
        }

        private void SetupMediatorForReport()
        {
            MediatorMock
                .Setup(x => x.Send(It.IsAny<GenerateTransactionsReport.Input>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://example.com/report.xlsx");
        }

        private void SetupMediatorForMarketReport()
        {
            MediatorMock
                .Setup(x => x.Send(It.IsAny<GenerateTransactionsReportForMarket.Input>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("https://example.com/report.xlsx");
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
            // Exige l'absence de toute erreur, pas seulement d'UnauthorizedAccessException: une
            // version antérieure de cette assertion laissait passer un ArgumentException levé par un
            // autre bug (identifiant de marché mal encodé dans la requête de test) en le confondant
            // avec un succès.
            result.Errors.Should().BeNullOrEmpty();
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
        // scope par champ que pour ce type précis, et les requêtes testées ici n'ont besoin d'aucune
        // isolation de scope entre champs.
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
