using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Serilog;
using Sig.App.Backend.Logging;

namespace Sig.App.Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging((context, loggingBuilder) =>
                {
                    var serilogLogger = SerilogLoggingExtensions
                        .ConfigureAllotomatSerilog(context.Configuration)
                        .CreateLogger();

                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddSerilog(serilogLogger, dispose: true);
                    loggingBuilder.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Debug);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
