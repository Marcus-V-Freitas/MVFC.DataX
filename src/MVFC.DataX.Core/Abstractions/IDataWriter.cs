namespace MVFC.DataX.Core.Abstractions;

/// <summary>
/// Defines a contract for writing data to a destination.
/// </summary>
/// <typeparam name="T">The type of the data to be written.</typeparam>
public interface IDataWriter<in T>
{
    /// <summary>
    /// Writes a single item asynchronously.
    /// </summary>
    /// <param name="item">The data item to write.</param>
    /// <param name="ct">A cancellation token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task WriteAsync(T item, CancellationToken ct = default);

    /// <summary>
    /// Writes a batch of items asynchronously.
    /// </summary>
    /// <param name="items">The collection of items to write.</param>
    /// <param name="ct">A cancellation token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task WriteBatchAsync(IReadOnlyList<T> items, CancellationToken ct = default);
}
