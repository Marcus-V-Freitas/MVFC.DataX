namespace MVFC.DataX.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class MongoIntegrationTests : IAsyncLifetime
{
    private readonly MongoDbContainer _mongo = new MongoDbBuilder("mongo:8.0").Build();

    static MongoIntegrationTests()
    {
        MongoConfig.Initialize();
    }

    public async ValueTask InitializeAsync() =>
        await _mongo.StartAsync(TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync() =>
        await _mongo.DisposeAsync().AsTask();

    [Fact]
    public async Task MongoIntegration_Reader_e_Writer_Deve_Funcionar()
    {
        // Arrange 1
        var writer1 = new MongoWriter<BankInfo>(_mongo.GetConnectionString(), "testdb", "banks");
        var reader1 = new MongoReader<BankInfo>(_mongo.GetConnectionString(), "testdb", "banks");
        var items1 = new[]
        {
            new BankInfo { Ispb = 1, Name = "B1", Code = 1 },
            new BankInfo { Ispb = 2, Name = "B2", Code = 2 }
        };

        // Act - Write
        await writer1.WriteAsync(new BankInfo { Ispb = 3, Name = "B3", Code = 3 }, TestContext.Current.CancellationToken);
        await writer1.WriteBatchAsync(items1, TestContext.Current.CancellationToken);

        // Act - Read
        var results1 = await reader1.ReadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        results1.Should().HaveCount(3);
        results1.Should().Contain(b => b.Code == 3);

        // Arrange 2
        var writer2 = new MongoWriter<BankInfo>(_mongo.GetConnectionString(), "testdb", "banks_filtered");
        var items2 = new[]
        {
            new BankInfo { Ispb = 1, Name = "A", Code = 1 },
            new BankInfo { Ispb = 2, Name = "B", Code = 2 },
            new BankInfo { Ispb = 3, Name = "C", Code = 3 },
        };
        await writer2.WriteBatchAsync(items2, TestContext.Current.CancellationToken);

        var filter = Builders<BankInfo>.Filter.Gt(b => b.Code, 1);
        var options = new FindOptions<BankInfo> { Limit = 1 };
        var reader2 = new MongoReader<BankInfo>(_mongo.GetConnectionString(), "testdb", "banks_filtered", filter, options);

        // Act
        var results2 = await reader2.ReadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        results2.Should().HaveCount(1);
        results2[0].Code.Should().BeGreaterThan(1);
    }
}
