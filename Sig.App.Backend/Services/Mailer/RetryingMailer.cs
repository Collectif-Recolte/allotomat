using Microsoft.Extensions.Logging;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Sig.App.Backend.Services.Mailer
{
    public class RetryingMailer : IMailer
    {
        private const int DelayBetweenRetriesInMs = 1000;
        private const int MaxRetries = 3;

        private readonly IMailer innerMailer;
        private readonly ILogger<RetryingMailer> logger;

        public RetryingMailer(IMailer innerMailer, ILogger<RetryingMailer> logger)
        {
            this.innerMailer = innerMailer;
            this.logger = logger;
        }

        public async Task Send<T>(T model) where T : EmailModel
        {
            var retryCount = 0;

            while (true)
            {
                try
                {
                    await innerMailer.Send(model);
                    return;
                }
                catch (Exception ex)
                {
                    if (!IsRetryable(ex))
                    {
                        logger.LogWarning($"Caught {ex.GetType().Name} but not retrying because error was not determined to be retryable.");
                        throw;
                    }

                    if (++retryCount > MaxRetries)
                    {
                        logger.LogWarning($"Caught {ex.GetType().Name} but not retrying because max retries exceeded.");
                        throw;
                    }

                    logger.LogInformation($"Caught {ex.GetType().Name}. Waiting {DelayBetweenRetriesInMs}ms and retrying ({retryCount}/{MaxRetries})");
                    await Task.Delay(DelayBetweenRetriesInMs);
                }
            }
        }

        private static bool IsRetryable(Exception exception)
        {
            switch (exception)
            {
                // Amazon SES retourne un code d'erreur 454 lorsque le rate limit est atteint
                case SmtpException se when se.StatusCode == SmtpStatusCode.ClientNotPermitted:
                    return true;
                default:
                    return false;
            }
        }
    }
}
