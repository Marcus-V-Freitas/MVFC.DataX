namespace MVFC.DataX.Core.Transformers;

public sealed class DistinctTransformer<T>(IEqualityComparer<T>? comparer = null) : IDataTransformer<T, T>
{
    private readonly IEqualityComparer<T> _comparer = comparer ?? EqualityComparer<T>.Default;

    public async IAsyncEnumerable<DataResult<T>> TransformAsync(
        IAsyncEnumerable<T> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var seen = new HashSet<T>(_comparer);
        
        await foreach (var item in input.WithCancellation(ct).ConfigureAwait(false))
        {
            if (item is null || !seen.Add(item))
            {
                continue;
            }

            yield return DataResult.Success(item);
        }
    }
}
