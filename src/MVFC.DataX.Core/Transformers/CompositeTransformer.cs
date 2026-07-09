namespace MVFC.DataX.Core.Transformers;

public sealed class CompositeTransformer<TIn, TMid, TOut> : IDataTransformer<TIn, TOut>
{
    private readonly IDataTransformer<TIn, TMid> _first;
    private readonly IDataTransformer<TMid, TOut> _second;

    public CompositeTransformer(IDataTransformer<TIn, TMid> first, IDataTransformer<TMid, TOut> second)
    {
        _first = first ?? throw new ArgumentNullException(nameof(first));
        _second = second ?? throw new ArgumentNullException(nameof(second));
    }

    public async IAsyncEnumerable<DataResult<TOut>> TransformAsync(
        IAsyncEnumerable<TIn> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var firstPass = _first.TransformAsync(input, ct);
        
        var secondInput = ExtractSuccessfulValues(firstPass, ct);
        
        await foreach (var result in _second.TransformAsync(secondInput, ct).ConfigureAwait(false))
        {
            yield return result;
        }
    }

    private static async IAsyncEnumerable<TMid> ExtractSuccessfulValues(
        IAsyncEnumerable<DataResult<TMid>> source,
        [EnumeratorCancellation] CancellationToken ct)
    {
        await foreach (var item in source.WithCancellation(ct).ConfigureAwait(false))
        {
            if (item.IsSuccess && item.Value is not null)
            {
                yield return item.Value;
            }
        }
    }
}
