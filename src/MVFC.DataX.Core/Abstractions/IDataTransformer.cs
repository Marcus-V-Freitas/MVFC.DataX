namespace MVFC.DataX.Core.Abstractions;

public interface IDataTransformer<in TInput, TOutput>
{
    public IAsyncEnumerable<DataResult<TOutput>> TransformAsync(
        IAsyncEnumerable<TInput> input,
        CancellationToken ct = default);
}
