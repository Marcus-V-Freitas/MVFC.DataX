namespace MVFC.DataX.Providers.RabbitMQ;

public sealed class RabbitMqDataReader<T>(
    IConnectionFactory connectionFactory,
    string queueName,
    Func<byte[], T> deserializer) : IDataReader<T>, IDisposable
{
    private readonly IConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    private readonly string _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
    private readonly Func<byte[], T> _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));

    private IConnection? _connection;
    private IChannel? _channel;

    public async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        _connection = await _connectionFactory.CreateConnectionAsync(ct).ConfigureAwait(false);
        _channel = await _connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);

        var channel = Channel.CreateUnbounded<T>();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var item = _deserializer(body);
                await channel.Writer.WriteAsync(item, ct).ConfigureAwait(false);
                await _channel.BasicAckAsync(ea.DeliveryTag, false, ct).ConfigureAwait(false);
            }
            catch
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false, ct).ConfigureAwait(false);
            }
        };

        var consumerTag = await _channel.BasicConsumeAsync(_queueName, false, consumer, ct).ConfigureAwait(false);

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            await _channel.BasicCancelAsync(consumerTag, cancellationToken: default).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
