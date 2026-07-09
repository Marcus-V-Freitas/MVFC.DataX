namespace MVFC.DataX.Core.Writers;

public sealed class DelegateWriter<T>(Func<IReadOnlyList<T>, CancellationToken, Task> batchWriter) : IDataWriter<T>
{
    private readonly Func<IReadOnlyList<T>, CancellationToken, Task> _batchWriter = batchWriter ?? throw new ArgumentNullException(nameof(batchWriter));

    public Task WriteAsync(T item, CancellationToken ct = default) =>
        _batchWriter([item], ct);

    public Task WriteBatchAsync(IReadOnlyList<T> items, CancellationToken ct = default) =>
        _batchWriter(items, ct);
}
