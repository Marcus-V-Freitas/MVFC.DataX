namespace MVFC.DataX.Core.Transformers;

public sealed class MapTransformer<TIn, TOut>(Func<TIn, TOut?> mapFunc) : IDataTransformer<TIn, TOut>
{
    private readonly Func<TIn, TOut?> _mapFunc = mapFunc ?? throw new ArgumentNullException(nameof(mapFunc));

    public async IAsyncEnumerable<DataResult<TOut>> TransformAsync(
        IAsyncEnumerable<TIn> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in input.WithCancellation(ct).ConfigureAwait(false))
        {
            TOut? mapped = default;
            Exception? ex = null;
            try
            {
                mapped = _mapFunc(item);
            }
            catch (Exception e)
            {
                ex = e;
            }

            if (ex != null)
            {
                yield return DataResult.Failure<TOut>([new DataError("Exception", ex.Message, item)]);
                continue;
            }

            if (mapped is null)
                continue;

            yield return DataResult.Success(mapped);
        }
    }
}
