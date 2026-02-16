using System.Text.Json;

namespace backend.Metrics;

public sealed class RequestMetrics
{
    // Latency buckets in ms: <=1, <=2, <=5, ... <=5000, >5000
    private static readonly int[] BoundsMs = { 1, 2, 5, 10, 20, 50, 100, 200, 500, 1000, 2000, 5000 };

    private long _total;
    private long _inflight;
    private long _status2xx;
    private long _status3xx;
    private long _status4xx;
    private long _status5xx;

    private long _sumLatencyMs;
    private long _maxLatencyMs;

    private readonly long[] _latencyBuckets = new long[BoundsMs.Length + 1];

    public void OnRequestStart() => Interlocked.Increment(ref _inflight);

    public void OnRequestEnd(int statusCode, long latencyMs)
    {
        Interlocked.Decrement(ref _inflight);
        Interlocked.Increment(ref _total);
        Interlocked.Add(ref _sumLatencyMs, latencyMs);

        long currentMax;
        while (latencyMs > (currentMax = Interlocked.Read(ref _maxLatencyMs)))
        {
            if (Interlocked.CompareExchange(ref _maxLatencyMs, latencyMs, currentMax) == currentMax)
                break;
        }

        switch (statusCode / 100)
        {
            case 2: Interlocked.Increment(ref _status2xx); break;
            case 3: Interlocked.Increment(ref _status3xx); break;
            case 4: Interlocked.Increment(ref _status4xx); break;
            case 5: Interlocked.Increment(ref _status5xx); break;
        }

        var idx = BucketIndex(latencyMs);
        Interlocked.Increment(ref _latencyBuckets[idx]);
    }

    public MetricsSnapshot SnapshotAndReset()
    {
        var total = Interlocked.Exchange(ref _total, 0);
        var inflight = Interlocked.Read(ref _inflight);

        var s2 = Interlocked.Exchange(ref _status2xx, 0);
        var s3 = Interlocked.Exchange(ref _status3xx, 0);
        var s4 = Interlocked.Exchange(ref _status4xx, 0);
        var s5 = Interlocked.Exchange(ref _status5xx, 0);

        var sum = Interlocked.Exchange(ref _sumLatencyMs, 0);
        var max = Interlocked.Exchange(ref _maxLatencyMs, 0);

        var buckets = new long[_latencyBuckets.Length];
        for (int i = 0; i < _latencyBuckets.Length; i++)
            buckets[i] = Interlocked.Exchange(ref _latencyBuckets[i], 0);

        var avg = total > 0 ? (double)sum / total : 0;

        var p50 = EstimatePercentileMs(buckets, 0.50);
        var p95 = EstimatePercentileMs(buckets, 0.95);

        return new MetricsSnapshot
        {
            TotalRequests = total,
            Inflight = inflight,
            Status2xx = s2,
            Status3xx = s3,
            Status4xx = s4,
            Status5xx = s5,
            AvgLatencyMs = avg,
            MaxLatencyMs = max,
            P50LatencyMs = p50,
            P95LatencyMs = p95,
            LatencyBuckets = FormatBuckets(buckets)
        };
    }

    private static int BucketIndex(long latencyMs)
    {
        for (int i = 0; i < BoundsMs.Length; i++)
        {
            if (latencyMs <= BoundsMs[i])
                return i;
        }
        return BoundsMs.Length;
    }

    private static long EstimatePercentileMs(long[] buckets, double p)
    {
        long total = 0;
        foreach (var c in buckets) total += c;
        if (total == 0) return 0;

        var target = (long)Math.Ceiling(total * p);
        long cum = 0;

        for (int i = 0; i < buckets.Length; i++)
        {
            cum += buckets[i];
            if (cum >= target)
            {
                if (i < BoundsMs.Length) return BoundsMs[i];
                return BoundsMs[^1] + 1; // indicates "over last bucket"
            }
        }

        return BoundsMs[^1] + 1;
    }

    private static Dictionary<string, long> FormatBuckets(long[] buckets)
    {
        var dict = new Dictionary<string, long>(buckets.Length);
        for (int i = 0; i < buckets.Length; i++)
        {
            if (i < BoundsMs.Length) dict[$"<= {BoundsMs[i]}ms"] = buckets[i];
            else dict[$"> {BoundsMs[^1]}ms"] = buckets[i];
        }
        return dict;
    }

    public static string ToJson(object obj) =>
        JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false });
}

public sealed class MetricsSnapshot
{
    public long TotalRequests { get; set; }
    public long Inflight { get; set; }
    public long Status2xx { get; set; }
    public long Status3xx { get; set; }
    public long Status4xx { get; set; }
    public long Status5xx { get; set; }

    public double AvgLatencyMs { get; set; }
    public long MaxLatencyMs { get; set; }
    public long P50LatencyMs { get; set; }
    public long P95LatencyMs { get; set; }

    public Dictionary<string, long> LatencyBuckets { get; set; } = new();
}
