namespace MVFC.DataX.Providers.PubSub;

public sealed class PubSubDataWriter<T>(
    PublisherClient publisherClient,
    Func<T, PubsubMessage> serializer) : IDataWriter<T>
{
    private readonly PublisherClient _publisherClient = publisherClient ?? throw new ArgumentNullException(nameof(publisherClient));
    private readonly Func<T, PubsubMessage> _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

    public async Task WriteAsync(T item, CancellationToken ct = default)
    {
        var message = _serializer(item);
        await _publisherClient.PublishAsync(message).ConfigureAwait(false);
    }

    public async Task WriteBatchAsync(IReadOnlyList<T> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
            return;

        var tasks = items.Select(item => _publisherClient.PublishAsync(_serializer(item)));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
