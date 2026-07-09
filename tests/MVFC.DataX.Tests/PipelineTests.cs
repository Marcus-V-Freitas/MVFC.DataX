namespace MVFC.DataX.Tests;

public sealed class PipelineTests : IAsyncLifetime
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder("mongo:8.0")
                                                        .Build();

    static PipelineTests()
    {
        MongoConfig.Initialize();
    }

    public ValueTask InitializeAsync() => new(_mongo.StartAsync());

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return new ValueTask(_mongo.DisposeAsync().AsTask());
    }

    [Fact]
    public async Task Pipeline_Deve_processar_fluxo_completo()
    {
        // Arrange
        var mockHttp = new MockHttpMessageHandler();
        var banks = new List<Bank>
        {
            new(123, "B1", 1, "B1"),
            new(null, "B2", 2, "B2"),
            new(456, "", 3, "B3"),
        };
        mockHttp.When("https://brasilapi.com.br/api/banks/v1").Respond("application/json", JsonSerializer.Serialize(banks));

        var client = mockHttp.ToHttpClient();
        client.BaseAddress = new Uri("https://brasilapi.com.br/api");
        var api = RestService.For<IBrasilApi>(client);

        var reader = new HttpApiReader<Bank>(api.GetBanksAsync);
        var writer = new MongoWriter<BankInfo>(_mongo.GetConnectionString(), "testdb", "banks");
        var dlqWriter = Substitute.For<IDataWriter<DataResult<BankInfo>>>();

        var pipeline = PipelineBuilder
            .ReadFrom(reader)
            .TransformWith(new FluentTransformer<Bank, BankInfo>(TestHelpers.MapBank, new BankInfoValidator()))
            .WriteTo(writer)
            .OnError(ErrorHandling.DeadLetter(dlqWriter))
            .WithParallelism(2)
            .WithBatchSize(1)
            .Build();

        // Act
        var stats = await pipeline.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        stats.Succeeded.Should().Be(1);
        stats.Failed.Should().Be(1);
        stats.Errors.Should().ContainSingle().Which.PropertyName.Should().Be("Name");

        var mongoClient = new MongoClient(_mongo.GetConnectionString());
        var collection = mongoClient.GetDatabase("testdb").GetCollection<BankInfo>("banks");
        var saved = await collection.Find(FilterDefinition<BankInfo>.Empty).ToListAsync(TestContext.Current.CancellationToken);
        saved.Should().ContainSingle().Which.Name.Should().Be("B1");

        await dlqWriter.Received(1).WriteAsync(Arg.Any<DataResult<BankInfo>>(), Arg.Any<CancellationToken>());
    }
}
