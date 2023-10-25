using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.Extensions;

namespace Sig.App.Backend.Plugins.MediatR
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            this.logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken,
            RequestHandlerDelegate<TResponse> next)
        {
            var requestName = GetRequestName();

            logger.LogTrace($"Handling {requestName}");

            var sw = Stopwatch.StartNew();

            try
            {
                var response = await next();
                sw.Stop();
                logger.LogTrace($"Handled {requestName} in {sw.ElapsedMilliseconds}ms");

                return response;
            }
            catch (Exception ex)
            {
                sw.Stop();

                switch (ex)
                {
                    case RequestValidationException _:
                    case IdentityResultException ire when ire.IsExpected():
                        logger.LogWarning(ex,
                            $"{requestName} failed with expected error after {sw.ElapsedMilliseconds}ms");
                        throw;
                    default:
                        logger.LogError(ex, $"{requestName} failed after {sw.ElapsedMilliseconds}ms");
                        throw;
                }
            }
        }

        private static string GetRequestName()
        {
            var requestType = typeof(TRequest);
            var requestName = requestType.Name;

            while (requestType.IsNested)
            {
                requestType = requestType.DeclaringType;
                requestName = $"{requestType.Name}/{requestName}";
            }

            return requestName;
        }
    }
}
