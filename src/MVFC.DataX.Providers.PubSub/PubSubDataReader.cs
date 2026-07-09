namespace MVFC.DataX.Providers.PubSub;

public sealed class PubSubDataReader<T>(
    SubscriberClient subscriberClient,
    Func<PubsubMessage, T> deserializer) : IDataReader<T>
{
    private readonly SubscriberClient _subscriberClient = subscriberClient ?? throw new ArgumentNullException(nameof(subscriberClient));
    private readonly Func<PubsubMessage, T> _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));

    public async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateUnbounded<T>();

        _ = _subscriberClient.StartAsync(async (message, cancellationToken) =>
        {
            try
            {
                var item = _deserializer(message);
                await channel.Writer.WriteAsync(item, cancellationToken).ConfigureAwait(false);
                return SubscriberClient.Reply.Ack;
            }
            catch (Exception)
            {
                return SubscriberClient.Reply.Nack;
            }
        });

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            await _subscriberClient.StopAsync(ct).ConfigureAwait(false);
        }
    }
}
