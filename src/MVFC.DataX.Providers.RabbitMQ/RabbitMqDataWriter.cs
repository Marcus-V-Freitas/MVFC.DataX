namespace MVFC.DataX.Providers.RabbitMQ;

public sealed class RabbitMqDataWriter<T>(
    IConnectionFactory connectionFactory,
    string exchange,
    string routingKey,
    Func<T, byte[]> serializer) : IDataWriter<T>, IDisposable
{
    private readonly IConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    private readonly string _exchange = exchange ?? throw new ArgumentNullException(nameof(exchange));
    private readonly string _routingKey = routingKey ?? throw new ArgumentNullException(nameof(routingKey));
    private readonly Func<T, byte[]> _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

    private IConnection? _connection;
    private IChannel? _channel;

    public async Task WriteAsync(T item, CancellationToken ct = default)
    {
        await EnsureConnectionAsync(ct).ConfigureAwait(false);

        var body = _serializer(item);
        var properties = new BasicProperties();

        await _channel!.BasicPublishAsync(_exchange, _routingKey, false, properties, body, ct).ConfigureAwait(false);
    }

    public async Task WriteBatchAsync(IReadOnlyList<T> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
            return;

        await EnsureConnectionAsync(ct).ConfigureAwait(false);

        foreach (var item in items)
        {
            var body = _serializer(item);
            var properties = new BasicProperties();
            await _channel!.BasicPublishAsync(_exchange, _routingKey, false, properties, body, ct).ConfigureAwait(false);
        }
    }

    private async Task EnsureConnectionAsync(CancellationToken ct)
    {
        if (_connection == null || _channel == null)
        {
            _connection = await _connectionFactory.CreateConnectionAsync(ct).ConfigureAwait(false);
            _channel = await _connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
