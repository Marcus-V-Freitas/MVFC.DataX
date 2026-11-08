namespace MVFC.DataX.Resilience;

public static class PipelineBuilderExtensions
{
    public static PipelineBuilder<TInput, TOutput> WithResiliencePolicy<TInput, TOutput>(
        this PipelineBuilder<TInput, TOutput> builder,
        ResiliencePipeline resiliencePipeline)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.ReplaceWriter(writer => new ResilientDataWriter<TOutput>(writer, resiliencePipeline));
    }

    public static PipelineBuilder<TInput, TOutput> WithPolly<TInput, TOutput>(
        this PipelineBuilder<TInput, TOutput> builder,
        ResiliencePipeline resiliencePipeline)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithResiliencePolicy(resiliencePipeline);
    }

    public static PipelineBuilder<TInput, TOutput> WithExponentialBackoff<TInput, TOutput>(
        this PipelineBuilder<TInput, TOutput> builder,
        int maxRetries,
        TimeSpan baseDelay,
        TimeSpan? maxDelay = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.WithExponentialBackoff(maxRetries, baseDelay, maxDelay);
    }
}
