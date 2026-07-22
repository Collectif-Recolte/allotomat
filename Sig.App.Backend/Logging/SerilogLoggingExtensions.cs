using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Elasticsearch;
using Sig.App.Backend.Configuration;

namespace Sig.App.Backend.Logging
{
    public static class SerilogLoggingExtensions
    {
        private const long HttpSinkQueueLimitBytes = 10 * 1024 * 1024;

        public static LoggerConfiguration ConfigureAllotomatSerilog(IConfiguration config)
        {
            var loggerConfiguration = new LoggerConfiguration();
            ApplyLogLevels(config, loggerConfiguration);

            loggerConfiguration
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "Allotomat")
                .WriteTo.Console();

            var axiomOptions = config.GetSection("Axiom").Get<AxiomOptions>();
            if (axiomOptions?.Enabled != true
                || string.IsNullOrWhiteSpace(axiomOptions.ApiToken)
                || string.IsNullOrWhiteSpace(axiomOptions.DatasetName))
            {
                return loggerConfiguration;
            }

            if (!string.IsNullOrWhiteSpace(axiomOptions.Environment))
            {
                loggerConfiguration.Enrich.WithProperty("environment", axiomOptions.Environment);
            }

            var domain = string.IsNullOrWhiteSpace(axiomOptions.Domain)
                ? "api.axiom.co"
                : axiomOptions.Domain.Trim().TrimEnd('/');

            var ingestBase = domain.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? $"{domain}/v1/ingest/{axiomOptions.DatasetName}"
                : $"https://{domain}/v1/ingest/{axiomOptions.DatasetName}";

            var ingestUri =
                $"{ingestBase}?timestamp-field={Uri.EscapeDataString(ElasticsearchJsonFormatter.TimestampPropertyName)}";

            loggerConfiguration.WriteTo.Http(
                requestUri: ingestUri,
                queueLimitBytes: HttpSinkQueueLimitBytes,
                textFormatter: new ElasticsearchJsonFormatter(renderMessageTemplate: false, inlineFields: true),
                httpClient: new AxiomHttpClient(axiomOptions.ApiToken));

            return loggerConfiguration;
        }

        private static void ApplyLogLevels(IConfiguration config, LoggerConfiguration loggerConfiguration)
        {
            var logLevelSection = config.GetSection("Logging:LogLevel");
            if (!logLevelSection.Exists())
            {
                loggerConfiguration.MinimumLevel.Warning();
                return;
            }

            var entries = logLevelSection.GetChildren().ToList();
            var defaultEntry = entries.FirstOrDefault(e =>
                string.Equals(e.Key, "Default", StringComparison.OrdinalIgnoreCase));

            loggerConfiguration.MinimumLevel.Is(
                defaultEntry != null && TryGetLogEventLevel(defaultEntry.Value, out var defaultLevel)
                    ? defaultLevel
                    : LogEventLevel.Warning);

            foreach (var entry in entries)
            {
                if (string.Equals(entry.Key, "Default", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (TryGetLogEventLevel(entry.Value, out var level))
                {
                    loggerConfiguration.MinimumLevel.Override(entry.Key, level);
                }
            }
        }

        private static bool TryGetLogEventLevel(string level, out LogEventLevel logEventLevel)
        {
            logEventLevel = default;

            if (string.IsNullOrWhiteSpace(level))
            {
                return false;
            }

            switch (level.ToUpperInvariant())
            {
                case "NONE":
                case "OFF":
                    logEventLevel = LevelAlias.Off;
                    return true;
                case "TRACE":
                    logEventLevel = LogEventLevel.Verbose;
                    return true;
                case "DEBUG":
                    logEventLevel = LogEventLevel.Debug;
                    return true;
                case "INFORMATION":
                    logEventLevel = LogEventLevel.Information;
                    return true;
                case "WARNING":
                    logEventLevel = LogEventLevel.Warning;
                    return true;
                case "ERROR":
                    logEventLevel = LogEventLevel.Error;
                    return true;
                case "CRITICAL":
                    logEventLevel = LogEventLevel.Fatal;
                    return true;
                default:
                    return Enum.TryParse(level, true, out logEventLevel);
            }
        }
    }
}
