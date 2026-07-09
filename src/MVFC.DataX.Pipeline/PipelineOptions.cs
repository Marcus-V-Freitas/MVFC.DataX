namespace MVFC.DataX.Pipeline;

public sealed record PipelineOptions
{
    public int Parallelism { get; init; } = 1;

    public int BatchSize { get; init; } = 100;

    public int ChannelCapacity { get; init; } = 1000;

    public int MaxRetries { get; init; }

    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);
}
