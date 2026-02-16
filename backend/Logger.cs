using Serilog;
using Serilog.Events;

namespace backend;

public static class LoggingConfiguration
{
    public static void ConfigureSerilog()
    {
        // Log to console
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message:lj}{NewLine}{Exception}"
            );

        // Optional file logging
        try
        {
            var logDir = Path.Combine(AppContext.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDir);

            loggerConfig = loggerConfig.WriteTo.File(
                Path.Combine(logDir, "APILog.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message:lj}{NewLine}{Exception}",
                buffered: false,
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                shared: true
            );
        }
        catch
        {
            // Ignore errors writing to disk. Console logging still works.
        }

        Log.Logger = loggerConfig.CreateLogger();
    }

    public static void ConfigureRequestLogging(IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            // Avoid query-string spam
            options.IncludeQueryInRequestPath = false;

            // W45-030 requirement: method, path, status, latency
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} => {StatusCode} in {Elapsed:0.0000} ms";

            // Suppress /health endpoint logs
            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                var path = httpContext.Request.Path.Value ?? "";

                if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
                    return LogEventLevel.Debug;

                if (ex != null)
                    return LogEventLevel.Error;

                var status = httpContext.Response.StatusCode;
                if (status >= 500)
                    return LogEventLevel.Error;

                return LogEventLevel.Information;
            };

            // Keep enrichment minimal
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
            };
        });
    }
}