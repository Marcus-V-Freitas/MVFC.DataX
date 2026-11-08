namespace MVFC.DataX.Pipeline;

internal sealed class IgnoreWriter<T> : IDataWriter<DataResult<T>>
{
    public Task WriteAsync(DataResult<T> item, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task WriteBatchAsync(IReadOnlyList<DataResult<T>> batch, CancellationToken ct = default) =>
        Task.CompletedTask;
}
