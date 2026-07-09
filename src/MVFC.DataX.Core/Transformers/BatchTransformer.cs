namespace MVFC.DataX.Core.Transformers;

public sealed class BatchTransformer<T>(int batchSize) : IDataTransformer<T, IReadOnlyList<T>>
{
    private readonly int _batchSize = batchSize > 0 ? batchSize : 100;

    public async IAsyncEnumerable<DataResult<IReadOnlyList<T>>> TransformAsync(
        IAsyncEnumerable<T> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var batch = new List<T>(_batchSize);

        await foreach (var item in input.WithCancellation(ct).ConfigureAwait(false))
        {
            batch.Add(item);
            if (batch.Count >= _batchSize)
            {
                yield return DataResult.Success<IReadOnlyList<T>>(batch);
                batch = new List<T>(_batchSize);
            }
        }

        if (batch.Count > 0)
        {
            yield return DataResult.Success<IReadOnlyList<T>>(batch);
        }
    }
}
