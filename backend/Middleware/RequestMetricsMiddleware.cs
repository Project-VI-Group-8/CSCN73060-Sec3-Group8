using System.Diagnostics;
using backend.Metrics;
using Microsoft.AspNetCore.Http;

namespace backend.Middleware;

public sealed class RequestMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RequestMetrics _metrics;

    public RequestMetricsMiddleware(RequestDelegate next, RequestMetrics metrics)
    {
        _next = next;
        _metrics = metrics;
    }

    public async Task Invoke(HttpContext context)
    {
        _metrics.OnRequestStart();

        var startTs = Stopwatch.GetTimestamp();

        // Initialize so the compiler always considers it assigned
        int statusCode = StatusCodes.Status200OK;

        try
        {
            await _next(context);
            statusCode = context.Response.StatusCode;
        }
        catch
        {
            statusCode = StatusCodes.Status500InternalServerError;
            throw;
        }
        finally
        {
            var elapsedMs = (long)((Stopwatch.GetTimestamp() - startTs) * 1000.0 / Stopwatch.Frequency);
            _metrics.OnRequestEnd(statusCode, elapsedMs);
        }
    }
}