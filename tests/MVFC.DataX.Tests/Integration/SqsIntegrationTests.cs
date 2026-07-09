namespace MVFC.DataX.Tests.Integration;

public sealed class SqsIntegrationTests : IAsyncLifetime
{
    private readonly LocalStackContainer _localStack = new LocalStackBuilder("localstack/localstack:3.8.1")
                                                            .Build();

    public async ValueTask InitializeAsync() =>
        await _localStack.StartAsync(TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync() =>
        await _localStack.DisposeAsync().AsTask();

    [Fact]
    public async Task WriteAndReadBatch_ShouldSucceed()
    {
        // Arrange
        var sqsConfig = new AmazonSQSConfig { ServiceURL = _localStack.GetConnectionString() };
        var sqsClient = new AmazonSQSClient("test", "test", sqsConfig);

        var createQueueResponse = await sqsClient.CreateQueueAsync(new CreateQueueRequest { QueueName = "test-queue" }, TestContext.Current.CancellationToken);
        var queueUrl = createQueueResponse.QueueUrl;

        var writer = new SqsDataWriter<User>(
            sqsClient,
            queueUrl,
            u => new SendMessageRequest { MessageBody = JsonSerializer.Serialize(u) });

        var reader = new SqsDataReader<User>(
            sqsClient,
            queueUrl,
            m => JsonSerializer.Deserialize<User>(m.Body)!,
            waitTimeSeconds: 1); // Fast wait for testing

        var results = new List<User>();

        // Act - Write single and empty
        await writer.WriteAsync(new User { Id = 3, Name = "Charlie" }, TestContext.Current.CancellationToken);
        await writer.WriteBatchAsync(Array.Empty<User>(), TestContext.Current.CancellationToken);

        // Act - Write batch
        await writer.WriteBatchAsync(
        [
            new User { Id = 1, Name = "Alice" },
            new User { Id = 2, Name = "Bob" }
        ], TestContext.Current.CancellationToken);

        // Act - Read
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await foreach (var item in reader.ReadAsync(cts.Token))
        {
            results.Add(item);
            if (results.Count == 3) break;
        }

        // Assert
        results.Should().HaveCount(3);
        results.Should().Contain(u => u.Name == "Charlie");
        results.Should().Contain(u => u.Name == "Alice");
        results.Should().Contain(u => u.Name == "Bob");
    }

    [Fact]
    public async Task ReadAsync_ShouldCancel()
    {
        // Arrange
        var sqsConfig = new AmazonSQSConfig { ServiceURL = _localStack.GetConnectionString() };
        var sqsClient = new AmazonSQSClient("test", "test", sqsConfig);

        var createQueueResponse = await sqsClient.CreateQueueAsync(new CreateQueueRequest { QueueName = "empty-queue" }, TestContext.Current.CancellationToken);

        var reader = new SqsDataReader<User>(sqsClient, createQueueResponse.QueueUrl, m => new User(), waitTimeSeconds: 1);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        // Act
        var act = async () =>
        {
            await foreach (var item in reader.ReadAsync(cts.Token)) { }
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
