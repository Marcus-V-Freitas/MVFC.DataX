namespace MVFC.DataX.Core.Transformers;

public sealed class FilterTransformer<T>(Func<T, bool> predicate) : IDataTransformer<T, T>
{
    private readonly Func<T, bool> _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));

    public async IAsyncEnumerable<DataResult<T>> TransformAsync(
        IAsyncEnumerable<T> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in input.WithCancellation(ct).ConfigureAwait(false))
        {
            var accept = false;
            Exception? ex = null;
            try
            {
                accept = _predicate(item);
            }
            catch (Exception e)
            {
                ex = e;
            }

            if (ex != null)
            {
                yield return DataResult.Failure<T>([new DataError("Exception", ex.Message, item)]);
                continue;
            }

            if (accept)
            {
                yield return DataResult.Success(item);
            }
        }
    }
}
