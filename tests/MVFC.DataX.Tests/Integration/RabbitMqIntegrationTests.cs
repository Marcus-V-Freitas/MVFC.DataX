namespace MVFC.DataX.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class RabbitMqIntegrationTests : IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitmq = new RabbitMqBuilder("rabbitmq:3.11")
                                                            .WithUsername("guest")
                                                            .WithPassword("guest")
                                                            .Build();

    public async ValueTask InitializeAsync() =>
        await _rabbitmq.StartAsync(TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync() =>
        await _rabbitmq.DisposeAsync().AsTask();

    [Fact]
    public async Task WriteAndReadBatch_ShouldSucceed()
    {
        // Arrange
        var factory = new ConnectionFactory { Uri = new Uri(_rabbitmq.GetConnectionString()) };

        using var connection = await factory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        using var channel = await connection.CreateChannelAsync(null, TestContext.Current.CancellationToken);

        var queueName = "test-queue";
        await channel.QueueDeclareAsync(queueName, false, false, false, null, false, TestContext.Current.CancellationToken);

        var writer = new RabbitMqDataWriter<User>(
            factory,
            exchange: string.Empty,
            routingKey: queueName,
            u => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(u)));

        var reader = new RabbitMqDataReader<User>(
            factory,
            queueName,
            b => JsonSerializer.Deserialize<User>(Encoding.UTF8.GetString(b))!);

        var results = new List<User>();

        // Act
        using var writerResource = writer;
        using var readerResource = reader;

        await writer.WriteAsync(new User { Id = 3, Name = "Charlie" }, TestContext.Current.CancellationToken);
        await writer.WriteBatchAsync([], TestContext.Current.CancellationToken);

        await writer.WriteBatchAsync(
        [
            new User { Id = 1, Name = "Alice" },
            new User { Id = 2, Name = "Bob" }
        ], TestContext.Current.CancellationToken);

        await foreach (var item in reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            results.Add(item);
            if (results.Count == 3)
            {
                break;
            }
        }

        // Assert
        results.Should().HaveCount(3);
        results[0].Name.Should().Be("Charlie");
        results[1].Name.Should().Be("Alice");
        results[2].Name.Should().Be("Bob");
    }

    [Fact]
    public async Task ReadAsync_ShouldCancel()
    {
        // Arrange
        var factory = new ConnectionFactory { Uri = new Uri(_rabbitmq.GetConnectionString()) };

        using var connection = await factory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        using var channel = await connection.CreateChannelAsync(null, TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync("empty-queue", false, false, false, null, false, TestContext.Current.CancellationToken);

        using var reader = new RabbitMqDataReader<User>(factory, "empty-queue", b => new User());
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500));

        // Act & Assert
        var act = async () =>
        {
            await foreach (var item in reader.ReadAsync(cts.Token)) { }
        };
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ReadAsync_InvalidMessage_ShouldCatchAndNack()
    {
        // Arrange
        var factory = new ConnectionFactory { Uri = new Uri(_rabbitmq.GetConnectionString()) };
        var queueName = "error-queue";

        using var connection = await factory.CreateConnectionAsync(TestContext.Current.CancellationToken);
        using var channel = await connection.CreateChannelAsync(null, TestContext.Current.CancellationToken);
        await channel.QueueDeclareAsync(queueName, false, false, false, null, false, TestContext.Current.CancellationToken);

        var writer = new RabbitMqDataWriter<User>(
            factory,
            exchange: string.Empty,
            routingKey: queueName,
            u => Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(u)));

        using var writerResource = writer;
        await writer.WriteBatchAsync(
        [
            new User { Id = 1, Name = "Bad1" },
            new User { Id = 2, Name = "Good" },
            new User { Id = 3, Name = "Bad2" }
        ], TestContext.Current.CancellationToken);

        using var reader = new RabbitMqDataReader<User>(
            factory,
            queueName,
            b =>
            {
                var u = System.Text.Json.JsonSerializer.Deserialize<User>(Encoding.UTF8.GetString(b))!;
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
