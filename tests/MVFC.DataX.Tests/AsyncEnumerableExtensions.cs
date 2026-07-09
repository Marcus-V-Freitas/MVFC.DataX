namespace MVFC.DataX.Tests;

public static class AsyncEnumerableExtensions
{
#if NET9_0
    public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source, CancellationToken cancellationToken = default)
    {
        var list = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            list.Add(item);
        }
        return list;
    }
#endif

    public static async IAsyncEnumerable<T> CreateAsyncEnumerable<T>(params T[] items)
    {
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }
}

