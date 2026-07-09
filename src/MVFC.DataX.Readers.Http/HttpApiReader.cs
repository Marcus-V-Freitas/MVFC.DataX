namespace MVFC.DataX.Readers.Http;

public sealed class HttpApiReader<T> : IDataReader<T>
{
    private readonly Func<CancellationToken, Task<IEnumerable<T>>>? _fetchEnumerable;
    private readonly Func<CancellationToken, IAsyncEnumerable<T>>? _fetchAsyncEnumerable;

    public HttpApiReader(Func<CancellationToken, Task<IEnumerable<T>>> fetch)
    {
        _fetchEnumerable = fetch ?? throw new ArgumentNullException(nameof(fetch));
    }

    public HttpApiReader(Func<CancellationToken, IAsyncEnumerable<T>> fetch)
    {
        _fetchAsyncEnumerable = fetch ?? throw new ArgumentNullException(nameof(fetch));
    }

    public async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_fetchAsyncEnumerable != null)
        {
            await foreach (var item in _fetchAsyncEnumerable(ct).WithCancellation(ct).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        else if (_fetchEnumerable != null)
        {
            var items = await _fetchEnumerable(ct).ConfigureAwait(false);
            foreach (var item in items)
            {
                ct.ThrowIfCancellationRequested();
                yield return item;
            }
        }
    }
}
