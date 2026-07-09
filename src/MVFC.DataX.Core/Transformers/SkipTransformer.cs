namespace MVFC.DataX.Core.Transformers;

public sealed class SkipTransformer<T>(int count) : IDataTransformer<T, T>
{
    private readonly int _count = count >= 0 ? count : 0;

    public async IAsyncEnumerable<DataResult<T>> TransformAsync(
        IAsyncEnumerable<T> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var skipped = 0;
        await foreach (var item in input.WithCancellation(ct).ConfigureAwait(false))
        {
            if (skipped < _count)
            {
                skipped++;
                continue;
            }

            yield return DataResult.Success(item);
        }
    }
}
