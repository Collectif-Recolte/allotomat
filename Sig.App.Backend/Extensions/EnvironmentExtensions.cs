using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Sig.App.Backend.Constants;

namespace Sig.App.Backend.Extensions
{
    public static class EnvironmentExtensions
    {
        public static bool IsEndToEnd(this IWebHostEnvironment env)
        {
            return env.IsEnvironment(AppEnvironments.EndToEndTesting);
        }
    }
}