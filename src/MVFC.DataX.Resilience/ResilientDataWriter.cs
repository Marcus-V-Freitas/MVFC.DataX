namespace MVFC.DataX.Resilience;

internal sealed class ResilientDataWriter<T>(IDataWriter<T> innerWriter, ResiliencePipeline pipeline) : IDataWriter<T>
{
    public ValueTask DisposeAsync()
    {
        if (innerWriter is IAsyncDisposable disposable)
        {
            return disposable.DisposeAsync();
        }

        if (innerWriter is IDisposable syncDisposable)
        {
            syncDisposable.Dispose();
        }

        return ValueTask.CompletedTask;
    }

    public async Task WriteAsync(T item, CancellationToken ct = default) =>
        await pipeline.ExecuteAsync(async (c) => await innerWriter.WriteAsync(item, c), ct).ConfigureAwait(false);

    public async Task WriteBatchAsync(IReadOnlyList<T> items, CancellationToken ct = default) =>
        await pipeline.ExecuteAsync(async (c) => await innerWriter.WriteBatchAsync(items, c), ct).ConfigureAwait(false);
}
