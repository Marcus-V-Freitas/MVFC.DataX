namespace MVFC.DataX.Tests.Integration;

public sealed class PubSubIntegrationTests : IAsyncLifetime
{
    private readonly PubSubContainer _pubsub = new PubSubBuilder("gcr.io/google.com/cloudsdktool/cloud-sdk:emulators")
                                                    .Build();

    public async ValueTask InitializeAsync() =>
        await _pubsub.StartAsync(TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync() =>
        await _pubsub.DisposeAsync().AsTask();

    [Fact]
    public async Task WriteAndReadBatch_ShouldSucceed()
    {
        // Arrange
        var projectId = "test-project";
        var topicId = "test-topic";
        var subscriptionId = "test-subscription";
        var emulatorEndpoint = _pubsub.GetEmulatorEndpoint();

        // Admin clients to create topic and sub
        var publisherService = await new PublisherServiceApiClientBuilder
        {
            Endpoint = emulatorEndpoint,
            ChannelCredentials = ChannelCredentials.Insecure
        }.BuildAsync(TestContext.Current.CancellationToken);

        var subscriberService = await new SubscriberServiceApiClientBuilder
        {
            Endpoint = emulatorEndpoint,
            ChannelCredentials = ChannelCredentials.Insecure
        }.BuildAsync(TestContext.Current.CancellationToken);

        var topicName = TopicName.FromProjectTopic(projectId, topicId);
        var subscriptionName = SubscriptionName.FromProjectSubscription(projectId, subscriptionId);

        await publisherService.CreateTopicAsync(topicName, TestContext.Current.CancellationToken);
        await subscriberService.CreateSubscriptionAsync(subscriptionName, topicName, pushConfig: null, ackDeadlineSeconds: 60, TestContext.Current.CancellationToken);

        // PublisherClient
        var publisherClient = await new PublisherClientBuilder
        {
            TopicName = topicName,
            Endpoint = emulatorEndpoint,
            ChannelCredentials = ChannelCredentials.Insecure
        }.BuildAsync(TestContext.Current.CancellationToken);

        var writer = new PubSubDataWriter<User>(
            publisherClient,
            u => new PubsubMessage { Data = Google.Protobuf.ByteString.CopyFromUtf8(JsonSerializer.Serialize(u)) });

        // SubscriberClient
        var subscriberClient = await new SubscriberClientBuilder
        {
            SubscriptionName = subscriptionName,
            Endpoint = emulatorEndpoint,
            ChannelCredentials = ChannelCredentials.Insecure
        }.BuildAsync(TestContext.Current.CancellationToken);

        var reader = new PubSubDataReader<User>(
            subscriberClient,
            m => JsonSerializer.Deserialize<User>(m.Data.ToStringUtf8())!);

        var results = new List<User>();

        // Act - Write single and empty
        await writer.WriteAsync(new User { Id = 3, Name = "Charlie" }, TestContext.Current.CancellationToken);
        await writer.WriteBatchAsync([], TestContext.Current.CancellationToken);

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
        var projectId = "test-project";
        var subscriptionId = "empty-subscription";
        var emulatorEndpoint = _pubsub.GetEmulatorEndpoint();

        // Note: we can just point to a non-existent sub or create one
        var subscriberClient = await new SubscriberClientBuilder
        {
            SubscriptionName = SubscriptionName.FromProjectSubscription(projectId, subscriptionId),
            Endpoint = emulatorEndpoint,
            ChannelCredentials = ChannelCredentials.Insecure
        }.BuildAsync(TestContext.Current.CancellationToken);

        var reader = new PubSubDataReader<User>(subscriberClient, m => new User());
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var act = async () =>
        {
            await foreach (var item in reader.ReadAsync(cts.Token)) { }
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ReadAsync_InvalidMessage_ShouldCatchAndNack()
    {
        // Arrange
        var projectId = "test-project";
        var topicId = "error-topic";
        var subscriptionId = "error-subscription";
        var emulatorEndpoint = _pubsub.GetEmulatorEndpoint();

        var publisherService = await new PublisherServiceApiClientBuilder { Endpoint = emulatorEndpoint, ChannelCredentials = ChannelCredentials.Insecure }.BuildAsync(TestContext.Current.CancellationToken);
        var subscriberService = await new SubscriberServiceApiClientBuilder { Endpoint = emulatorEndpoint, ChannelCredentials = ChannelCredentials.Insecure }.BuildAsync(TestContext.Current.CancellationToken);

        var topicName = TopicName.FromProjectTopic(projectId, topicId);
        var subscriptionName = SubscriptionName.FromProjectSubscription(projectId, subscriptionId);

        await publisherService.CreateTopicAsync(topicName, TestContext.Current.CancellationToken);
        await subscriberService.CreateSubscriptionAsync(subscriptionName, topicName, pushConfig: null, ackDeadlineSeconds: 10, TestContext.Current.CancellationToken);

        var publisherClient = await new PublisherClientBuilder { TopicName = topicName, Endpoint = emulatorEndpoint, ChannelCredentials = ChannelCredentials.Insecure }.BuildAsync(TestContext.Current.CancellationToken);
        var writer = new PubSubDataWriter<User>(
            publisherClient,
            u => new PubsubMessage { Data = Google.Protobuf.ByteString.CopyFromUtf8(System.Text.Json.JsonSerializer.Serialize(u)) });

        await writer.WriteBatchAsync(
        [
            new User { Id = 1, Name = "Bad1" },
            new User { Id = 2, Name = "Good" },
            new User { Id = 3, Name = "Bad2" }
        ], TestContext.Current.CancellationToken);

        var subscriberClient = await new SubscriberClientBuilder { SubscriptionName = subscriptionName, Endpoint = emulatorEndpoint, ChannelCredentials = ChannelCredentials.Insecure }.BuildAsync(TestContext.Current.CancellationToken);
        var reader = new PubSubDataReader<User>(
            subscriberClient,
            m =>
            {
                var u = System.Text.Json.JsonSerializer.Deserialize<User>(m.Data.ToStringUtf8())!;
                if (u.Name!.StartsWith("Bad", StringComparison.OrdinalIgnoreCase))
                    throw new Exception("Bad message");
                return u;
            });

        // Act
        var results = new List<User>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await foreach (var item in reader.ReadAsync(cts.Token))
        {
            results.Add(item);
            if (results.Count == 1) break;
        }

        // Assert
        results.Should().HaveCount(1);
        results[0].Name.Should().Be("Good");
    }
}
