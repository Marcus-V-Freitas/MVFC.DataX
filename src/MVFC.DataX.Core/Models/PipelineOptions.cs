namespace MVFC.DataX.Core.Models;

public sealed record PipelineOptions
{
    public int Parallelism { get; init; } = 1;
    public int BatchSize { get; init; } = 100;
    public int ChannelCapacity { get; init; } = 1000;
    public int MaxRetries { get; init; }
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);
    public Func<Exception, bool>? RetryPredicate { get; init; }
    public bool UseJitter { get; init; } = true;
    public bool UseExponentialBackoff { get; init; }
    public TimeSpan? MaxRetryDelay { get; init; }
    public Action<Exception, int, TimeSpan>? OnRetry { get; init; }
    public Func<Exception, ErrorAction>? ErrorClassifier { get; init; }
}
