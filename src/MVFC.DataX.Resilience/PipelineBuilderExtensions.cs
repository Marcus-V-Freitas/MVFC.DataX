namespace MVFC.DataX.Resilience;

public static class PipelineBuilderExtensions
{
    public static PipelineBuilder<TInput, TOutput> WithResiliencePolicy<TInput, TOutput>(
        this PipelineBuilder<TInput, TOutput> builder,
        ResiliencePipeline resiliencePipeline)
    {
        return builder.ReplaceWriter(writer => new ResilientDataWriter<TOutput>(writer, resiliencePipeline));
    }

    public static PipelineBuilder<TInput, TOutput> WithPolly<TInput, TOutput>(
        this PipelineBuilder<TInput, TOutput> builder,
        ResiliencePipeline resiliencePipeline)
    {
        return builder.WithResiliencePolicy(resiliencePipeline);
    }

    public static PipelineBuilder<TInput, TOutput> WithExponentialBackoff<TInput, TOutput>(
        this PipelineBuilder<TInput, TOutput> builder,
        int maxRetries,
        TimeSpan baseDelay,
        TimeSpan? maxDelay = null)
    {
        return builder.WithExponentialBackoff(maxRetries, baseDelay, maxDelay);
    }
}

internal sealed class ResilientDataWriter<T>(IDataWriter<T> innerWriter, ResiliencePipeline pipeline) : IDataWriter<T>
{
    public ValueTask DisposeAsync()
    {
        if (innerWriter is IAsyncDisposable disposable)
        {
            return disposable.DisposeAsync();
        }
        else if (innerWriter is IDisposable syncDisposable)
        {
            syncDisposable.Dispose();
        }
        return ValueTask.CompletedTask;
    }

    public async Task WriteAsync(T item, CancellationToken ct = default)
    {
        await pipeline.ExecuteAsync(async (c) => await innerWriter.WriteAsync(item, c), ct).ConfigureAwait(false);
    }

    public async Task WriteBatchAsync(IReadOnlyList<T> items, CancellationToken ct = default)
    {
        await pipeline.ExecuteAsync(async (c) => await innerWriter.WriteBatchAsync(items, c), ct).ConfigureAwait(false);
    }
}
