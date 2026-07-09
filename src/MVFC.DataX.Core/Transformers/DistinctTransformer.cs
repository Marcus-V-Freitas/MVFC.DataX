namespace MVFC.DataX.Core.Transformers;

/// <summary>
/// Remove itens duplicados do stream.
/// </summary>
/// <remarks>
/// IMPORTANTE: Sem um limite de capacidade (maxCapacity), este transformer acumula todos os itens distintos lidos
/// na memória em um HashSet. Para streams muito grandes ou infinitos, utilize a opção de capacidade máxima (sliding window)
/// para evitar estouro de memória (OOM).
/// </remarks>
public sealed class DistinctTransformer<T>(IEqualityComparer<T>? comparer = null, int? maxCapacity = null) : IDataTransformer<T, T>
{
    private readonly IEqualityComparer<T> _comparer = comparer ?? EqualityComparer<T>.Default;
    private readonly int? _maxCapacity = maxCapacity;

    public async IAsyncEnumerable<DataResult<T>> TransformAsync(
        IAsyncEnumerable<T> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var seen = new HashSet<T>(_comparer);
        var queue = _maxCapacity.HasValue ? new Queue<T>(_maxCapacity.Value) : null;
        
        await foreach (var item in input.WithCancellation(ct).ConfigureAwait(false))
        {
            if (item is null)
            {
                continue;
            }

            if (seen.Add(item))
            {
                UpdateSlidingWindow(item, seen, queue);
                yield return DataResult.Success(item);
            }
        }
    }

    private void UpdateSlidingWindow(T item, HashSet<T> seen, Queue<T>? queue)
    {
        if (queue == null || !_maxCapacity.HasValue)
        {
            return;
        }

        queue.Enqueue(item);
        if (queue.Count > _maxCapacity.Value)
        {
            var oldest = queue.Dequeue();
            if (oldest is not null)
            {
                seen.Remove(oldest);
            }
        }
    }
}
