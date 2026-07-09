namespace MVFC.DataX.Core.Abstractions;

/// <summary>
/// Defines a contract for reading data asynchronously from a source.
/// </summary>
/// <typeparam name="T">The type of the data to be read.</typeparam>
public interface IDataReader<out T>
{
    /// <summary>
    /// Reads data asynchronously and returns an <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    /// <param name="ct">A cancellation token to monitor for cancellation requests.</param>
    /// <returns>An asynchronous stream of data items.</returns>
    public IAsyncEnumerable<T> ReadAsync(CancellationToken ct = default);
}
