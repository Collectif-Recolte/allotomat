using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Extensions;
using System.Threading.Tasks;
using System.Threading;
using System;
using System.Linq;

namespace Sig.App.Backend.Plugins.MediatR;

public class ConcurrencyHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private const int MaxRetries = 3;

    private readonly ILogger<ConcurrencyHandlingBehavior<TRequest, TResponse>> logger;
    private readonly AppDbContext db;

    public ConcurrencyHandlingBehavior(ILogger<ConcurrencyHandlingBehavior<TRequest, TResponse>> logger, AppDbContext db)
    {
        this.logger = logger;
        this.db = db;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var retryCount = 0;

        while (true)
        {
            try
            {
                return await next();
            }
            catch (Exception ex)
            {
                if (!CanHandle(ex)) throw;

                // Can't use generic constraint `where TRequest: ISafeToRetry` because it's not supported by dotnet's DI container.
                if (!(request is ISafeToRetry))
                {
                    logger.LogWarning("Caught {ExceptionType} but not retrying because request is not marked as ISafeToRetry", ex.GetType().Name);
                    throw;
                }

                if (retryCount >= MaxRetries)
                {
                    logger.LogWarning("Caught {ExceptionType} but not retrying because MaxRetries ({MaxRetries}) exceeded", ex.GetType().Name, MaxRetries);
                    throw;
                }

                retryCount++;

                logger.LogInformation("Caught {ExceptionType}. Retrying operation ({RetryCount} out of {MaxRetries} maximum retries)", ex.GetType().Name, retryCount, MaxRetries);
                db.RejectChanges(detachAll: true);
            }
        }
    }

    private bool CanHandle(Exception ex)
    {
        switch (ex)
        {
            case DbUpdateException _:
            case IdentityResultException ire
                when ire.IdentityResult.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.ConcurrencyFailure)):
                return true;
            default:
                return false;
        }
    }
}