namespace MVFC.DataX.Pipeline;

internal sealed class DeadLetterWriter<T>(IDataWriter<DataResult<T>> innerWriter) : IDataWriter<DataResult<T>>
{
    public Task WriteAsync(DataResult<T> item, CancellationToken ct = default) =>
        innerWriter.WriteAsync(item, ct);

    public Task WriteBatchAsync(IReadOnlyList<DataResult<T>> batch, CancellationToken ct = default) =>
        innerWriter.WriteBatchAsync(batch, ct);
}
