namespace MVFC.DataX.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class PostgresIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:15.1")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpassword")
        .Build();

    public async ValueTask InitializeAsync() =>
        await _postgres.StartAsync(TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync() =>
        await _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task WriteAndReadBatch_ShouldSucceed()
    {
        // Arrange
        var connectionString = _postgres.GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var cmd = new NpgsqlCommand("CREATE TABLE users (id INT, name VARCHAR(100))", connection);
        await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        var writer = new PostgresDataWriter<User>(
            connectionString,
            "INSERT INTO users (id, name) VALUES (@id, @name)",
            (c, u) =>
            {
                c.Parameters.AddWithValue("id", u.Id);
                c.Parameters.AddWithValue("name", u.Name);
            });

        var reader = new PostgresDataReader<User>(
            connectionString,
            "SELECT id, name FROM users ORDER BY id",
            r => new User { Id = r.GetInt32(0), Name = r.GetString(1) });

        // Act
        await writer.WriteAsync(new User { Id = 3, Name = "Charlie" }, TestContext.Current.CancellationToken);
        await writer.WriteBatchAsync(
        [
            new User { Id = 1, Name = "Alice" },
            new User { Id = 2, Name = "Bob" }
        ], TestContext.Current.CancellationToken);

        var results = await reader.ReadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        results.Should().HaveCount(3);
        results[0].Name.Should().Be("Alice");
        results[1].Name.Should().Be("Bob");
        results[2].Name.Should().Be("Charlie");
    }

    [Fact]
    public async Task WriteBatch_EmptyAndError_ShouldBeHandled()
    {
        // Arrange
        var connectionString = _postgres.GetConnectionString();
        var writer = new PostgresDataWriter<User>(
            connectionString,
            "INSERT INTO NO_TABLE (id, name) VALUES (@id, @name)",
            (c, u) => { });

        // Act - Empty
        await writer.WriteBatchAsync(Array.Empty<User>(), TestContext.Current.CancellationToken); // should just return

        // Act - Error
        Func<Task> act = () => writer.WriteBatchAsync(
            [new User { Id = 1, Name = "Alice" }], 
            TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ReadAsync_EmptyTable_E_ConfigureCommand_ShouldSucceed()
    {
        // Arrange
        var connectionString = _postgres.GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var cmd = new NpgsqlCommand("CREATE TABLE empty_users (id INT, name VARCHAR(100))", connection);
        await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        var reader = new PostgresDataReader<User>(
            connectionString,
            "SELECT id, name FROM empty_users",
            r => new User(),
            c => c.CommandTimeout = 30); // configureCommand preenchido

        // Act
        var results = await reader.ReadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadAsync_MapperLancaExcecao_DevePropagarELiberarRecursos()
    {
        // Arrange
        var connectionString = _postgres.GetConnectionString();
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var cmd = new NpgsqlCommand("CREATE TABLE fail_users (id INT, name VARCHAR(100))", connection);
        await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        
        await using var insert = new NpgsqlCommand("INSERT INTO fail_users VALUES (1, 'Alice')", connection);
        await insert.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        var reader = new PostgresDataReader<User>(
            connectionString,
            "SELECT id, name FROM fail_users",
            r => throw new InvalidOperationException("Erro no mapper"));

        // Act
        var act = async () =>
        {
            await foreach (var item in reader.ReadAsync(TestContext.Current.CancellationToken))
            {
                // do nothing
            }
        };

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
