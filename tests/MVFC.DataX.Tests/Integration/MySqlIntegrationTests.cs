namespace MVFC.DataX.Tests.Integration;

public sealed class MySqlIntegrationTests : IAsyncLifetime
{
    private readonly MySqlContainer _mysql = new MySqlBuilder("mysql:8.0")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpassword")
        .Build();

    public async ValueTask InitializeAsync() =>
        await _mysql.StartAsync(TestContext.Current.CancellationToken);

    public async ValueTask DisposeAsync() =>
        await _mysql.DisposeAsync().AsTask();

    [Fact]
    public async Task WriteAndReadBatch_ShouldSucceed()
    {
        // Arrange
        var connectionString = _mysql.GetConnectionString();
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var cmd = new MySqlCommand("CREATE TABLE users (id INT, name VARCHAR(100))", connection);
        await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        var writer = new MySqlDataWriter<User>(
            connectionString,
            "INSERT INTO users (id, name) VALUES (@id, @name)",
            (c, u) =>
            {
                c.Parameters.AddWithValue("@id", u.Id);
                c.Parameters.AddWithValue("@name", u.Name);
            });

        var reader = new MySqlDataReader<User>(
            connectionString,
            "SELECT id, name FROM users ORDER BY id",
            r => new User { Id = r.GetInt32(0), Name = r.GetString(1) });

        // Act
        await writer.WriteAsync(new User { Id = 3, Name = "Charlie" }, TestContext.Current.CancellationToken);
        await writer.WriteBatchAsync(
        [
            new User { Id = 1, Name = "Alice" },
            new User { Id = 2, Name = "Bob" },
        ], TestContext.Current.CancellationToken);

        var results = await reader.ReadAsync(TestContext.Current.CancellationToken)
                                  .ToListAsync(TestContext.Current.CancellationToken);

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
        var connectionString = _mysql.GetConnectionString();
        var writer = new MySqlDataWriter<User>(
            connectionString,
            "INSERT INTO NO_TABLE (id, name) VALUES (@id, @name)",
            (c, u) => { });

        // Act - Empty
        await writer.WriteBatchAsync([], TestContext.Current.CancellationToken);

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
        var connectionString = _mysql.GetConnectionString();
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var cmd = new MySqlCommand("CREATE TABLE empty_users (id INT, name VARCHAR(100))", connection);
        await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        var reader = new MySqlDataReader<User>(
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
        var connectionString = _mysql.GetConnectionString();
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var cmd = new MySqlCommand("CREATE TABLE fail_users (id INT, name VARCHAR(100))", connection);
        await cmd.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        
        await using var insert = new MySqlCommand("INSERT INTO fail_users VALUES (1, 'Alice')", connection);
        await insert.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

        var reader = new MySqlDataReader<User>(
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
