namespace MVFC.DataX.Core.Abstractions;

public interface IDataReader<out T>
{
    public IAsyncEnumerable<T> ReadAsync(CancellationToken ct = default);
}
