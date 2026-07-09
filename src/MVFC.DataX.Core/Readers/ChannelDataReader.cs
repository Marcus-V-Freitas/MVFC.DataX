namespace MVFC.DataX.Core.Readers;

public sealed class ChannelDataReader<T>(ChannelReader<T> channelReader) : IDataReader<T>
{
    private readonly ChannelReader<T> _channelReader = channelReader ?? throw new ArgumentNullException(nameof(channelReader));

    public async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in _channelReader.ReadAllAsync(ct).ConfigureAwait(false))
        {
            yield return item;
        }
    }
}
