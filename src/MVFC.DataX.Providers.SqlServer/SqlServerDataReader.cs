namespace MVFC.DataX.Providers.SqlServer;

public sealed class SqlServerDataReader<T>(
    string connectionString,
    string query,
    Func<SqlDataReader, T> mapper,
    Action<SqlCommand>? configureCommand = null) : IDataReader<T>
{
    private readonly string _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    private readonly string _query = query ?? throw new ArgumentNullException(nameof(query));
    private readonly Func<SqlDataReader, T> _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly Action<SqlCommand>? _configureCommand = configureCommand;

    public async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await using var command = new SqlCommand(_query, connection);
        _configureCommand?.Invoke(command);

        await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);

        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            yield return _mapper(reader);
        }
    }
}
