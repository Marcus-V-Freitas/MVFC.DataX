namespace MVFC.DataX.Core.Transformers;

public sealed class PassthroughTransformer<T> : IDataTransformer<T, T>
{
    public async IAsyncEnumerable<DataResult<T>> TransformAsync(
        IAsyncEnumerable<T> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in input.WithCancellation(ct).ConfigureAwait(false))
        {
            yield return DataResult.Success(item);
        }
    }
}
