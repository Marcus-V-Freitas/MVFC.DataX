namespace MVFC.DataX.Core.Transformers;

public sealed class TakeTransformer<T>(int count) : IDataTransformer<T, T>
{
    private readonly int _count = count >= 0 ? count : 0;

    public async IAsyncEnumerable<DataResult<T>> TransformAsync(
        IAsyncEnumerable<T> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_count == 0)
        {
            yield break;
        }

        var taken = 0;
        await foreach (var item in input.WithCancellation(ct).ConfigureAwait(false))
        {
            yield return DataResult.Success(item);
            taken++;

            if (taken >= _count)
            {
                break;
            }
        }
    }
}
