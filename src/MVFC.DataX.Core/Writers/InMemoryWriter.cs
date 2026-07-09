namespace MVFC.DataX.Core.Writers;

public sealed class InMemoryWriter<T> : IDataWriter<T>
{
    private readonly ConcurrentBag<T> _items = [];

    public IReadOnlyCollection<T> Items => _items;

    public Task WriteAsync(T item, CancellationToken ct = default)
    {
        _items.Add(item);
        return Task.CompletedTask;
    }

    public Task WriteBatchAsync(IReadOnlyList<T> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            _items.Add(item);
        }
        return Task.CompletedTask;
    }
}
