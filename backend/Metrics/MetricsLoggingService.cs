using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace backend.Metrics;

public sealed class MetricsLoggingService : BackgroundService
{
    private readonly ILogger<MetricsLoggingService> _logger;
    private readonly RequestMetrics _metrics;
    private readonly int _intervalSeconds;

    public MetricsLoggingService(
        ILogger<MetricsLoggingService> logger,
        RequestMetrics metrics,
        IConfiguration configuration)
    {
        _logger = logger;
        _metrics = metrics;
        _intervalSeconds = configuration.GetValue<int?>("Metrics:LogIntervalSeconds") ?? 10;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_intervalSeconds <= 0) return;

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_intervalSeconds));

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var snap = _metrics.SnapshotAndReset();

                var payload = new
                {
                    type = "metrics",
                    tsUtc = DateTimeOffset.UtcNow,
                    intervalSeconds = _intervalSeconds,
                    totalRequests = snap.TotalRequests,
                    inflight = snap.Inflight,
                    status2xx = snap.Status2xx,
                    status3xx = snap.Status3xx,
                    status4xx = snap.Status4xx,
                    status5xx = snap.Status5xx,
                    avgLatencyMs = snap.AvgLatencyMs,
                    p50LatencyMs = snap.P50LatencyMs,
                    p95LatencyMs = snap.P95LatencyMs,
                    maxLatencyMs = snap.MaxLatencyMs,
                    latencyBuckets = snap.LatencyBuckets
                };

                // Interval-only output (NOT per request)
                _logger.LogInformation(RequestMetrics.ToJson(payload));
            }
            catch (Exception ex)
            {
                // Metrics must never crash the server
                _logger.LogError(ex, "Metrics logging failed");
            }
        }
    }
}