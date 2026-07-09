namespace MVFC.DataX.Core.Readers;

public sealed class EnumerableReader<T> : IDataReader<T>
{
    private readonly IEnumerable<T>? _syncEnumerable;
    private readonly IAsyncEnumerable<T>? _asyncEnumerable;

    public EnumerableReader(IEnumerable<T> source)
    {
        _syncEnumerable = source ?? throw new ArgumentNullException(nameof(source));
    }

    public EnumerableReader(IAsyncEnumerable<T> source)
    {
        _asyncEnumerable = source ?? throw new ArgumentNullException(nameof(source));
    }

    public async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_asyncEnumerable is not null)
        {
            await foreach (var item in _asyncEnumerable.WithCancellation(ct).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        else if (_syncEnumerable is not null)
        {
            foreach (var item in _syncEnumerable)
            {
                ct.ThrowIfCancellationRequested();
                yield return item;
                await Task.Yield();
            }
        }
    }
}
