

namespace MVFC.DataX.Tests.Integration;

public sealed class FileSystemIntegrationTests
{
    [Fact]
    public async Task Csv_WriteAndReadBatch_ShouldSucceed()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".csv");
        var reader = new CsvDataReader<User>(tempFile);

        try
        {
            // Act
            await using (var writer = new CsvDataWriter<User>(tempFile))
            {
                await writer.WriteBatchAsync(
                [
                    new User { Id = 1, Name = "Alice" },
                    new User { Id = 2, Name = "Bob" }
                ], TestContext.Current.CancellationToken);
            }

            var results = await reader.ReadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

            // Assert
            results.Should().HaveCount(2);
            results[0].Name.Should().Be("Alice");
            results[1].Name.Should().Be("Bob");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Csv_WriteAsync_And_EmptyBatch_ShouldSucceed()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".csv");
        var reader = new CsvDataReader<User>(tempFile);

        try
        {
            // Act
            await using (var writer = new CsvDataWriter<User>(tempFile))
            {
                await writer.WriteAsync(new User { Id = 1, Name = "SingleItem" }, TestContext.Current.CancellationToken);
                await writer.WriteBatchAsync([], TestContext.Current.CancellationToken);
            }

            var results = await reader.ReadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

            // Assert
            results.Should().ContainSingle();
            results[0].Name.Should().Be("SingleItem");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Jsonl_WriteAndReadBatch_ShouldSucceed()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".jsonl");
        var reader = new JsonlDataReader<User>(tempFile);

        try
        {
            // Act
            await using (var writer = new JsonlDataWriter<User>(tempFile))
            {
                await writer.WriteBatchAsync(
                [
                    new User { Id = 1, Name = "Alice" },
                    new User { Id = 2, Name = "Bob" }
                ], TestContext.Current.CancellationToken);
            }

            var results = await reader.ReadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

            // Assert
            results.Should().HaveCount(2);
            results[0].Name.Should().Be("Alice");
            results[1].Name.Should().Be("Bob");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task Jsonl_WriteAsync_And_EmptyBatch_ShouldSucceed()
    {
        // Arrange
        var tempFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".jsonl");
        var reader = new JsonlDataReader<User>(tempFile);

        try
        {
            // Act
            await using (var writer = new JsonlDataWriter<User>(tempFile))
            {
                await writer.WriteAsync(new User { Id = 1, Name = "SingleItem" }, TestContext.Current.CancellationToken);
                await writer.WriteBatchAsync([], TestContext.Current.CancellationToken);
            }

            var results = await reader.ReadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

            // Assert
            results.Should().ContainSingle();
            results[0].Name.Should().Be("SingleItem");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
