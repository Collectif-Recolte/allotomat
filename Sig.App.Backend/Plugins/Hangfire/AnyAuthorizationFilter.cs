using System.Linq;
using Hangfire.Dashboard;

namespace Sig.App.Backend.Plugins.Hangfire
{
    public class AnyAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly IDashboardAuthorizationFilter[] innerFilters;

        public AnyAuthorizationFilter(params IDashboardAuthorizationFilter[] innerFilters)
        {
            this.innerFilters = innerFilters;
        }

        public bool Authorize(DashboardContext context)
        {
            return innerFilters.Any(x => x.Authorize(context));
        }
    }
}