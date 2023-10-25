using System.Linq;
using Hangfire.Dashboard;
using Microsoft.Extensions.Logging;

namespace Sig.App.Backend.Plugins.Hangfire
{
    public class IpWhitelistAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly string[] whitelist;
        private readonly ILogger<IpWhitelistAuthorizationFilter> logger;

        public IpWhitelistAuthorizationFilter(string[] whitelist, ILogger<IpWhitelistAuthorizationFilter> logger)
        {
            this.whitelist = whitelist;
            this.logger = logger;
        }

        public IpWhitelistAuthorizationFilter(string whitelist, ILogger<IpWhitelistAuthorizationFilter> logger)
        {
            this.logger = logger;
            this.whitelist = whitelist
                .Split(';')
                .Select(x => x.Trim())
                .ToArray();
        }

        public bool Authorize(DashboardContext context)
        {
            var authorized = whitelist.Contains(context.Request.RemoteIpAddress);

            if (!authorized)
            {
                logger.LogInformation($"IP not in whitelist: {context.Request.RemoteIpAddress}");
            }

            return authorized;
        }
    }
}