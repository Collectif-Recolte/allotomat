using Hangfire.Dashboard;

namespace Sig.App.Backend.Plugins.Hangfire
{
    public class AllowEveryoneAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return true;
        }
    }
}