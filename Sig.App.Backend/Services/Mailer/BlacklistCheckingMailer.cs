using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Sig.App.Backend.Services.Mailer
{
    public class BlacklistCheckingMailer : IMailer
    {
        private readonly IMailer innerMailer;
        private readonly IEmailBlacklistService blacklist;
        private readonly ILogger<BlacklistCheckingMailer> logger;

        public BlacklistCheckingMailer(IMailer innerMailer, IEmailBlacklistService blacklist, ILogger<BlacklistCheckingMailer> logger)
        {
            this.innerMailer = innerMailer;
            this.blacklist = blacklist;
            this.logger = logger;
        }

        public Task Send<T>(T model) where T : EmailModel
        {
            var recipient = model.To;

            if (blacklist.IsBlacklisted(recipient))
            {
                logger.LogWarning($"Not sending email to {recipient} because it is blacklisted.");
                return Task.CompletedTask;
            }

            return innerMailer.Send(model);
        }
    }
}