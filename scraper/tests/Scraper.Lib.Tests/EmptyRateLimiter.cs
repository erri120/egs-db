using System.Threading.RateLimiting;

namespace Scraper.Lib.Tests;

public class EmptyRateLimiter : RateLimiter
{
    private static RateLimitLease _lease = new EmptyRateLimitLease();

    public override RateLimiterStatistics? GetStatistics() => null;

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
    {
        return _lease;
    }

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(_lease);
    }

    public override TimeSpan? IdleDuration => null;
}

public class EmptyRateLimitLease : RateLimitLease
{
    public override bool TryGetMetadata(string metadataName, out object? metadata)
    {
        metadata = null;
        return false;
    }

    public override bool IsAcquired => true;
    public override IEnumerable<string> MetadataNames => Array.Empty<string>();
}

