namespace MVFC.DataX.Providers.SQS;

public sealed class SqsDataReader<T>(
    IAmazonSQS sqsClient,
    string queueUrl,
    Func<Message, T> deserializer,
    int maxMessages = 10,
    int waitTimeSeconds = 20) : IDataReader<T>
{
    private readonly IAmazonSQS _sqsClient = sqsClient ?? throw new ArgumentNullException(nameof(sqsClient));
    private readonly string _queueUrl = queueUrl ?? throw new ArgumentNullException(nameof(queueUrl));
    private readonly Func<Message, T> _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
    private readonly int _maxMessages = maxMessages;
    private readonly int _waitTimeSeconds = waitTimeSeconds;

    public async IAsyncEnumerable<T> ReadAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            var request = new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = _maxMessages,
                WaitTimeSeconds = _waitTimeSeconds
            };

            var response = await _sqsClient.ReceiveMessageAsync(request, ct).ConfigureAwait(false);

            foreach (var message in response.Messages)
            {
                ct.ThrowIfCancellationRequested();

                T item;
                try
                {
                    item = _deserializer(message);
                }
                catch
                {
                    continue;
                }

                yield return item;

                await _sqsClient.DeleteMessageAsync(_queueUrl, message.ReceiptHandle, ct).ConfigureAwait(false);
            }
        }

        ct.ThrowIfCancellationRequested();
    }
}
