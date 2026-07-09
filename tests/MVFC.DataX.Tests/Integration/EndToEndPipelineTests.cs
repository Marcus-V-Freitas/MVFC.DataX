namespace MVFC.DataX.Tests.Integration;

public sealed class EndToEndPipelineTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:15.1")
        .WithDatabase("e2e_db")
        .WithUsername("e2e_user")
        .WithPassword("e2e_password")
        .Build();

    public async ValueTask InitializeAsync() =>
        await _postgres.StartAsync(TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync() =>
        await _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task PostgresToFile_ShouldProcessPipelineCorrectly()
    {
        // Arrange
        var connectionString = _postgres.GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var cmd = new NpgsqlCommand("CREATE TABLE products (id INT, name VARCHAR(100), price DECIMAL)", connection);
        await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        await using var insertCmd = new NpgsqlCommand("INSERT INTO products (id, name, price) VALUES (1, 'Laptop', 1000), (2, 'Mouse', 50), (3, 'Keyboard', 80)", connection);
        await insertCmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        var reader = new PostgresDataReader<Product>(
            connectionString,
            "SELECT id, name, price FROM products",
            r => new Product { Id = r.GetInt32(0), Name = r.GetString(1), Price = r.GetDecimal(2) });

        var tempCsvFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".csv");
        var tempDlqFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".jsonl");

        try
        {
            // Act
            await using (var writer = new CsvDataWriter<ProductResult>(tempCsvFile))
            await using (var dlqWriter = new JsonlDataWriter<DataResult<ProductResult>>(tempDlqFile))
            {
                var pipeline = PipelineBuilder.ReadFrom(reader)
                .TransformWith(new FilterTransformer<Product>(p => p.Price > 60))
                .TransformWith(new MapTransformer<Product, ProductResult>(p => new ProductResult { Id = p.Id, Description = $"{p.Name} - ${p.Price}" }))
                .WriteTo(writer)
                .OnError(dlqWriter)
                .WithParallelism(2)
                .WithBatchSize(2)
                .Build();

                var stats = await pipeline.RunAsync(TestContext.Current.CancellationToken);

                stats.Succeeded.Should().Be(2);
                stats.Failed.Should().Be(0);
            }

            // Assert
            var csvReader = new CsvDataReader<ProductResult>(tempCsvFile);
            var results = await csvReader.ReadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

            results.Should().HaveCount(2);
            results.Select(r => r.Description).Should().Contain("Laptop - $1000");
            results.Select(r => r.Description).Should().Contain("Keyboard - $80");
        }
        finally
        {
            if (File.Exists(tempCsvFile)) File.Delete(tempCsvFile);
            if (File.Exists(tempDlqFile)) File.Delete(tempDlqFile);
        }
    }
}
