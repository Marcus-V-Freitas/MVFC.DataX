namespace MVFC.DataX.Providers.SqlServer;

public sealed class SqlServerDataWriter<T>(
    string connectionString,
    string commandText,
    Action<SqlCommand, T> bindParameters) : IDataWriter<T>
{
    private readonly string _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    private readonly string _commandText = commandText ?? throw new ArgumentNullException(nameof(commandText));
    private readonly Action<SqlCommand, T> _bindParameters = bindParameters ?? throw new ArgumentNullException(nameof(bindParameters));

    public async Task WriteAsync(T item, CancellationToken ct = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await using var command = new SqlCommand(_commandText, connection);
        _bindParameters(command, item);

        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    public async Task WriteBatchAsync(IReadOnlyList<T> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
            return;

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(ct).ConfigureAwait(false);

        try
        {
            foreach (var item in items)
            {
                ct.ThrowIfCancellationRequested();

                await using var command = new SqlCommand(_commandText, connection, transaction);
                _bindParameters(command, item);
                await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            await transaction.CommitAsync(ct).ConfigureAwait(false);
        }
        catch
        {
            await transaction.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }
}
