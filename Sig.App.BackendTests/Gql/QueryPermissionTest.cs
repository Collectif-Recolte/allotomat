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
    // [RequirePermission] is a GraphQL.Conventions execution filter: it only runs inside the schema's
    // resolution pipeline, never in the handler. The only way to prove it blocks a call is therefore
    // to route a real query through the actual GraphQL engine, not to call the MediatR handler directly.
    //
    // The engine is shared between calls within a single test method, never rebuilt per call, and it
    // stays that way now that !5436 has landed. FILETS-32 was a `private bool hasPermission` on
    // RequirePermissionAttribute that was never reset: once any call was accepted, the singleton engine
    // that Startup.cs registers in production answered "allowed" for every later call. !5436 moved that
    // state into locals, so the field is gone — but the only configuration in which it was ever visible
    // is the shared engine, so rebuilding one per call here would give up the net that catches the next
    // instance field somebody adds. For GenerateTransactionsReport the refused call therefore still runs
    // before the accepted call on the SAME engine: under FILETS-32 the reverse order hid the refusal,
    // and keeping this order is what makes the test fail again if that state ever comes back.
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

        // Id.New<Market>(1), i.e. "Market:1" encoded. GraphQL.Conventions encodes the C# type name
        // with its original casing (uppercase): a lowercase "market:1" identifier, as an earlier
        // version of this test used, matches no type and fails decoding before the guard is even
        // reached, which an ArgumentException should have exposed.
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

        // Id.New<Market>(2), distinct from the market managed by the merchant in the tests below
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

        // AUDIT-01/FILETS-30: GenerateTransactionsReport exposed no guard and any authenticated
        // account could call it. The required guard is GlobalPermission.ManageTransactions, the same
        // one as the TransactionLogs screen it reproduces.
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

        // FILETS-30: GenerateTransactionsReportForMarket exposed no guard. A merchant must be able to
        // export the report of THEIR OWN market ("Exporter un rapport" button in the merchant screen).
        [Fact]
        public async Task GenerateTransactionsReportForMarket_MerchantIsAcceptedOnOwnMarket()
        {
            var merchant = AddUser("merchant@example.com", UserType.Merchant, claims: new[] { new Claim(AppClaimTypes.MarketManagerOf, "1") });

            SetupMediatorForMarketReport();

            var result = await ExecuteAsync(BuildEngine(), GenerateTransactionsReportForMarketOwnMarketQuery, merchant);

            AssertSucceeded(result);
        }

        // FILETS-30: without a guard, nothing verified that the requested market is the caller's own.
        // A merchant managing market 1 must not be able to export the report of market 2.
        [Fact]
        public async Task GenerateTransactionsReportForMarket_MerchantIsRefusedOnOtherMarket()
        {
            var merchant = AddUser("merchant@example.com", UserType.Merchant, claims: new[] { new Claim(AppClaimTypes.MarketManagerOf, "1") });

            SetupMediatorForMarketReport();

            var result = await ExecuteAsync(BuildEngine(), GenerateTransactionsReportForMarketOtherMarketQuery, merchant);

            AssertRefused(result);
        }

        // FILETS-30: the manual guard checked market permissions but not account status, unlike
        // RequirePermissionAttribute which refuses a user whose Status is not UserStatus.Actived before
        // even looking at permissions. A disabled account managing its own market must still be refused.
        [Fact]
        public async Task GenerateTransactionsReportForMarket_DisabledMerchantIsRefusedOnOwnMarket()
        {
            var merchant = AddUser("merchant@example.com", UserType.Merchant, claims: new[] { new Claim(AppClaimTypes.MarketManagerOf, "1") });
            merchant.Status = UserStatus.Disabled;
            await DbContext.SaveChangesAsync();

            SetupMediatorForMarketReport();

            var result = await ExecuteAsync(BuildEngine(), GenerateTransactionsReportForMarketOwnMarketQuery, merchant);

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
            // Requires the absence of any error, not just of UnauthorizedAccessException: an earlier
            // version of this assertion let an ArgumentException raised by an unrelated bug (a market
            // id badly encoded in the test query) slip through as a success.
            result.Errors.Should().BeNullOrEmpty();
        }

        private static Exception UnwrapException(ExecutionError error)
        {
            var exception = error.InnerException;
            return exception is FieldResolutionException ? exception.InnerException : exception;
        }

        private static GraphQLEngine BuildEngine()
        {
            // GraphQLEngineFactory.Create() reflects the schema's [ApplyPolicy]/[AnnotatePolicy]
            // classes at build time, which requires an IAuthorizationPolicyProvider already in place
            // (see Startup.cs, which does the same thing before calling GraphQLEngineFactory.Create()).
            var authServices = new ServiceCollection();
            authServices.AddLogging();
            authServices.AddAuthorization();
            var authProvider = authServices.BuildServiceProvider().GetRequiredService<IAuthorizationPolicyProvider>();
            AnnotatePolicyAttribute.AuthorizationPolicyProvider = authProvider;

            return GraphQLEngineFactory.Create();
        }

        // Minimal IDependencyInjector resolving directly against a flat IServiceProvider. Deliberately
        // distinct from Sig.App.Backend.Plugins.GraphQL.DependencyInjector: ScopedFieldResolver only
        // creates a per-field scope for that exact type, and the queries tested here need no scope
        // isolation between fields.
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
