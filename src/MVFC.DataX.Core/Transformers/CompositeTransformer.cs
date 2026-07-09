namespace MVFC.DataX.Core.Transformers;

public sealed class CompositeTransformer<TIn, TMid, TOut>(IDataTransformer<TIn, TMid> first, IDataTransformer<TMid, TOut> second) : IDataTransformer<TIn, TOut>
{
    private readonly IDataTransformer<TIn, TMid> _first = first ?? throw new ArgumentNullException(nameof(first));
    private readonly IDataTransformer<TMid, TOut> _second = second ?? throw new ArgumentNullException(nameof(second));

    public async IAsyncEnumerable<DataResult<TOut>> TransformAsync(
        IAsyncEnumerable<TIn> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var firstPass = _first.TransformAsync(input, ct);

        await foreach (var midResult in firstPass.WithCancellation(ct).ConfigureAwait(false))
        {
            if (midResult.IsFailure)
            {
                yield return DataResult.Failure<TOut>(midResult.Errors);
                continue;
            }

            if (midResult.Value is null)
                continue;

            await foreach (var outResult in _second.TransformAsync(SingleItemAsync(midResult.Value, ct), ct).ConfigureAwait(false))
            {
                yield return outResult;
            }
        }
    }

    private static async IAsyncEnumerable<T> SingleItemAsync<T>(
        T item, [EnumeratorCancellation] CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        yield return item;
    }
}
