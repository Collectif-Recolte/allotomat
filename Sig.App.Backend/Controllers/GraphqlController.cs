using GraphQL;
using GraphQL.Conventions;
using GraphQL.Conventions.Execution;
using GraphQL.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using GraphQLParser.AST;

namespace Sig.App.Backend.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class GraphqlController : ControllerBase
    {
        private readonly GraphQLEngine engine;
        private readonly IDependencyInjector injector;
        private readonly IUserContext userContext;
        private readonly ILogger<GraphqlController> logger;

        private readonly List<IValidationRule> validationRules;

        public GraphqlController(GraphQLEngine engine, IDependencyInjector injector, IUserContext userContext, ILogger<GraphqlController> logger, IWebHostEnvironment environment)
        {
            this.engine = engine;
            this.injector = injector;
            this.userContext = userContext;
            this.logger = logger;

            validationRules = new List<IValidationRule>();
            if (environment.IsProduction())
                validationRules.Add(new NoIntrospection());
        }

        public async Task<IActionResult> Post()
        {
            using (logger.BeginScope(Guid.NewGuid()))
            {
                var requestBody = await ReadStream(Request.Body);

                var executor = engine
                    .NewExecutor()
                    .WithValidationRules(validationRules)
                    .WithUserContext(userContext)
                    .WithDependencyInjector(injector)
                    .WithRequest(requestBody);

                var sw = Stopwatch.StartNew();
                var executionResult = await executor.ExecuteAsync();
                sw.Stop();

                var result = Content(engine.SerializeResult(executionResult), "application/json");
                result.StatusCode = StatusCodes.Status200OK;

                LogExecutionOutcome(executionResult, requestBody, sw);

                return result;
            }
        }

        private void LogExecutionOutcome(ExecutionResult executionResult, string requestBody, Stopwatch stopwatch)
        {
            var hasError = executionResult.Errors?.Any() ?? false;
            var hasUnexpectedError = executionResult.Errors?.Any(x => !IsExpectedError(x)) ?? false;

            var operationDescription = GetOperationDescription(executionResult.Operation, requestBody);

            (string desc, LogLevel level) outcome =
                hasUnexpectedError ? ("with errors", LogLevel.Error)
                        : hasError ? ("with expected errors", LogLevel.Warning)
                                   : ("successfully", LogLevel.Trace);

            logger.Log(outcome.level, $"{operationDescription} ran {outcome.desc} in {stopwatch.ElapsedMilliseconds}ms.");

            LogExecutionErrors(executionResult.Errors);
        }

        private static string GetOperationDescription(GraphQLOperationDefinition op, string requestBody)
        {
            if (op != null)
                return $"GQL {op.Operation} {(string.IsNullOrWhiteSpace(op.Name.StringValue) ? "(anonymous)" : op.Name)}";

            try
            {
                var query = new RequestDeserializer().GetQueryFromRequestBody(requestBody);
                var operationName = string.IsNullOrWhiteSpace(query.OperationName) ? "(anonymous)" : query.OperationName;
                return $"GQL operation {operationName}";
            }
            catch
            {
                return "Unknown GQL operation";
            }
        }

        private void LogExecutionErrors(ExecutionErrors errors)
        {
            if (errors == null) return;

            foreach (var error in errors)
            {
                if (IsExpectedError(error))
                    logger.LogWarning(error, "Expected error in graphql");
                else
                    logger.LogError(error, "Error in graphql");
            }
        }

        private bool IsExpectedError(ExecutionError error)
        {
            var actualException = error.InnerException;
            if (actualException is FieldResolutionException)
                actualException = actualException.InnerException;

            switch (actualException)
            {
                case RequestValidationException _:
                case IdentityResultException ire when ire.IsExpected():
                    return true;
                default:
                    return false;
            }
        }

        private async Task<string> ReadStream(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}
