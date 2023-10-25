using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Extensions;

namespace Sig.App.Backend.Plugins.MediatR
{
    public class ConcurrencyHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private const int MaxRetries = 3;

        private readonly ILogger<ConcurrencyHandlingBehavior<TRequest, TResponse>> logger;
        private readonly AppDbContext db;

        public ConcurrencyHandlingBehavior(ILogger<ConcurrencyHandlingBehavior<TRequest, TResponse>> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, RequestHandlerDelegate<TResponse> next)
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
                        logger.LogWarning($"Caught {ex.GetType().Name} but not retrying because request is not marked as ISafeToRetry");
                        throw;
                    }

                    if (retryCount >= MaxRetries)
                    {
                        logger.LogWarning($"Caught {ex.GetType().Name} but not retrying because MaxRetries ({MaxRetries}) exceeded");
                        throw;
                    }

                    retryCount++;

                    logger.LogInformation($"Caught {ex.GetType().Name}. Retrying operation ({retryCount} out of {MaxRetries} maximum retries)");
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
}
