namespace MVFC.DataX.Core.Readers;

public interface IQueryableReader<out T, in TQuery> : IDataReader<T>
{
    public IAsyncEnumerable<T> ReadAsync(TQuery query, CancellationToken ct = default);
}
