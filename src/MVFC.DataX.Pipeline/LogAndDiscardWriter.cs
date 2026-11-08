namespace MVFC.DataX.Pipeline;

internal sealed class LogAndDiscardWriter<T>(Action<DataResult<T>> logAction) : IDataWriter<DataResult<T>>
{
    public Task WriteAsync(DataResult<T> item, CancellationToken ct = default)
    {
        logAction(item);
        return Task.CompletedTask;
    }

    public Task WriteBatchAsync(IReadOnlyList<DataResult<T>> batch, CancellationToken ct = default)
    {
        foreach (var item in batch) logAction(item);
        return Task.CompletedTask;
    }
}
