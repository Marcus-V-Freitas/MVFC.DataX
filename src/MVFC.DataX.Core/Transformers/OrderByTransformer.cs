namespace MVFC.DataX.Core.Transformers;

/// <summary>
/// Ordena os itens em memória.
/// </summary>
/// <remarks>
/// IMPORTANTE: Este transformer acumula todos os itens lidos na memória antes de ordenar e prosseguir. 
/// Deve ser utilizado com precaução em pipelines que lidam com grandes volumes de dados para evitar estouro de memória (OOM).
/// </remarks>
public sealed class OrderByTransformer<T, TKey>(Func<T, TKey> keySelector, bool descending = false, IComparer<TKey>? comparer = null) : IDataTransformer<T, T>
{
    private readonly Func<T, TKey> _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    private readonly bool _descending = descending;
    private readonly IComparer<TKey> _comparer = comparer ?? Comparer<TKey>.Default;

    public async IAsyncEnumerable<DataResult<T>> TransformAsync(
        IAsyncEnumerable<T> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var buffer = new List<T>();

        await foreach (var item in input.WithCancellation(ct).ConfigureAwait(false))
        {
            buffer.Add(item);
        }

        if (buffer.Count == 0)
        {
            yield break;
        }

        if (_descending)
        {
            buffer.Sort((x, y) => _comparer.Compare(_keySelector(y), _keySelector(x)));
        }
        else
        {
            buffer.Sort((x, y) => _comparer.Compare(_keySelector(x), _keySelector(y)));
        }

        foreach (var item in buffer)
        {
            ct.ThrowIfCancellationRequested();
            yield return DataResult.Success(item);
        }
    }
}
