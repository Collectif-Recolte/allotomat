﻿using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using StackifyLib.CoreLogger;

namespace Sig.App.Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .ConfigureLogging(config =>
                {
                    config.AddFilter<ApplicationInsightsLoggerProvider>("", LogLevel.Debug);
                    config.AddProvider(new StackifyLoggerProvider());
                });
    }
}
