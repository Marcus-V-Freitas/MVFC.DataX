namespace MVFC.DataX.Core.Transformers;

public sealed class AggregateTransformer<TIn, TAccumulate>(
    TAccumulate seed,
    Func<TAccumulate, TIn, TAccumulate> func) : IDataTransformer<TIn, TAccumulate>
{
    private readonly TAccumulate _seed = seed;
    private readonly Func<TAccumulate, TIn, TAccumulate> _func = func ?? throw new ArgumentNullException(nameof(func));

    public async IAsyncEnumerable<DataResult<TAccumulate>> TransformAsync(
        IAsyncEnumerable<TIn> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var current = _seed;
        Exception? ex = null;
        object? failedItem = null;

        await foreach (var item in input.WithCancellation(ct).ConfigureAwait(false))
        {
            try
            {
                current = _func(current, item);
            }
            catch (Exception e)
            {
                ex = e;
                failedItem = item;
                break;
            }
        }

        yield return ex != null
            ? DataResult.Failure<TAccumulate>([new DataError("Exception", ex.Message, failedItem)])
            : DataResult.Success(current);
    }
}
