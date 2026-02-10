using Serilog;
using Serilog.Events;

namespace backend;

public static class LoggingConfiguration
{
    public static void ConfigureSerilog()
    {
        Directory.CreateDirectory("Logs");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                "Logs/APILog.txt",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}",
                buffered: false,
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                shared: true
            )
            .CreateLogger();
    }

    public static void ConfigureRequestLogging(IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} from {RemoteIpAddress} responded {StatusCode} in {Elapsed:0.0000} ms";
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
                diagnosticContext.Set("Host", httpContext.Request.Host.Value);
                diagnosticContext.Set("Protocol", httpContext.Request.Protocol);
                diagnosticContext.Set("Scheme", httpContext.Request.Scheme);
                diagnosticContext.Set("QueryString", httpContext.Request.QueryString.Value);
                diagnosticContext.Set("ContentType", httpContext.Request.ContentType);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
            };
        });
    }
}