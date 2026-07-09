namespace MVFC.DataX.Providers.SQS;

public sealed class SqsDataWriter<T>(
    IAmazonSQS sqsClient,
    string queueUrl,
    Func<T, SendMessageRequest> serializer) : IDataWriter<T>
{
    private readonly IAmazonSQS _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
    private readonly string _queueUrl = queueUrl ?? throw new ArgumentNullException(nameof(queueUrl));
    private readonly Func<T, SendMessageRequest> _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

    public async Task WriteAsync(T item, CancellationToken ct = default)
    {
        var request = _serializer(item);
        request.QueueUrl = _queueUrl;

        await _sqsClient.SendMessageAsync(request, ct).ConfigureAwait(false);
    }

    public async Task WriteBatchAsync(IReadOnlyList<T> items, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
            return;

        var batches = items.Chunk(10);
        foreach (var batch in batches)
        {
            ct.ThrowIfCancellationRequested();

            var entries = batch.Select((item, index) =>
            {
                var req = _serializer(item);
                return new SendMessageBatchRequestEntry(index.ToString(CultureInfo.InvariantCulture), req.MessageBody)
                {
                    DelaySeconds = req.DelaySeconds,
                    MessageAttributes = req.MessageAttributes,
                    MessageDeduplicationId = req.MessageDeduplicationId,
                    MessageGroupId = req.MessageGroupId
                };
            }).ToList();

            var request = new SendMessageBatchRequest
            {
                QueueUrl = _queueUrl,
                Entries = entries
            };

            var response = await _sqsClient.SendMessageBatchAsync(request, ct).ConfigureAwait(false);

            if (response.Failed.Count > 0)
            {
                throw new InvalidOperationException($"Failed to write {response.Failed.Count} items to SQS");
            }
        }
    }
}
