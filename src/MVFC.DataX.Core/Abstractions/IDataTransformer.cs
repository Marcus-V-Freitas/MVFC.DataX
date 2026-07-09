namespace MVFC.DataX.Core.Abstractions;

/// <summary>
/// Defines a contract for transforming data asynchronously in a pipeline.
/// </summary>
/// <typeparam name="TInput">The type of the input data.</typeparam>
/// <typeparam name="TOutput">The type of the transformed output data.</typeparam>
public interface IDataTransformer<in TInput, TOutput>
{
    /// <summary>
    /// Transforms an asynchronous stream of input data into an asynchronous stream of <see cref="DataResult{TOutput}"/>.
    /// </summary>
    /// <param name="input">The stream of input data.</param>
    /// <param name="ct">A cancellation token to monitor for cancellation requests.</param>
    /// <returns>An asynchronous stream containing the transformation results.</returns>
    public IAsyncEnumerable<DataResult<TOutput>> TransformAsync(
        IAsyncEnumerable<TInput> input,
        CancellationToken ct = default);
}
