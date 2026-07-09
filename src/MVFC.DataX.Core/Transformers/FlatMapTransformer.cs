namespace MVFC.DataX.Core.Transformers;

public sealed class FlatMapTransformer<TIn, TOut>(Func<TIn, IEnumerable<TOut>?> mapFunc) : IDataTransformer<TIn, TOut>
{
    private readonly Func<TIn, IEnumerable<TOut>?> _mapFunc = mapFunc ?? throw new ArgumentNullException(nameof(mapFunc));

    public async IAsyncEnumerable<DataResult<TOut>> TransformAsync(
        IAsyncEnumerable<TIn> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in input.WithCancellation(ct).ConfigureAwait(false))
        {
            IEnumerable<TOut>? mapped = null;
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
                yield return DataResult.Failure<TOut>([DataError.FromException(ex, attemptedValue: item)]);
                continue;
            }

            if (mapped is null)
            {
                yield return DataResult.Failure<TOut>([new DataError("Mapping", "Mapping returned null", item)]);
                continue;
            }

            foreach (var outItem in mapped)
            {
                ct.ThrowIfCancellationRequested();
                if (outItem is not null)
                {
                    yield return DataResult.Success(outItem);
                }
            }
        }
    }
}
