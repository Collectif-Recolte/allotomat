using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.Extensions;

namespace Sig.App.Backend.Plugins.MediatR;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        this.logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = GetRequestName();

        logger.LogTrace("Handling {RequestName}", requestName);

        var sw = Stopwatch.StartNew();

        try
        {
            var response = await next();
            sw.Stop();
            logger.LogTrace("Handled {RequestName} in {Millis}ms", requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();

            switch (ex)
            {
                case RequestValidationException _:
                case IdentityResultException ire when ire.IsExpected():
                    logger.LogWarning(ex, "{RequestName} failed with expected error after {Millis}ms", requestName, sw.ElapsedMilliseconds);
                    throw;
                default:
                    logger.LogError(ex, "{RequestName} failed after {Millis}ms", requestName, sw.ElapsedMilliseconds);
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
            if (requestType == null) break;

            requestName = $"{requestType.Name}/{requestName}";
        }

        return requestName;
    }
}