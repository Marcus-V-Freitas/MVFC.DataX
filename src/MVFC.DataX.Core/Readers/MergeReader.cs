namespace MVFC.DataX.Core.Readers;

/// <summary>
/// Lê de múltiplos leitores sequencialmente.
/// </summary>
public sealed class MergeReader<T>(params IDataReader<T>[] readers) : IDataReader<T>
{
    private readonly IDataReader<T>[] _readers = readers ?? throw new ArgumentNullException(nameof(readers));

    public async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        foreach (var reader in _readers)
        {
            await foreach (var item in reader.ReadAsync(ct).ConfigureAwait(false))
            {
                yield return item;
            }
        }
    }
}
