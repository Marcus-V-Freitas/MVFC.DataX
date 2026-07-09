namespace MVFC.DataX.Core.Abstractions;

public interface IDataWriter<in T>
{
    public Task WriteAsync(T item, CancellationToken ct = default);

    public Task WriteBatchAsync(IReadOnlyList<T> items, CancellationToken ct = default);
}
