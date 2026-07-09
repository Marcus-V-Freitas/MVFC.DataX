namespace MVFC.DataX.Tests.Readers;

public sealed class HttpApiReaderTests
{
    [Fact]
    public async Task ReadAsync_WithEnumerable_ShouldYieldItems()
    {
        // Arrange
        var reader = new HttpApiReader<int>(async ct =>
        {
            await Task.Delay(1, ct);
            return [1, 2, 3];
        });

        var results = new List<int>();

        // Act
        await foreach (var item in reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().BeEquivalentTo([1, 2, 3], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task ReadAsync_WithAsyncEnumerable_ShouldYieldItems()
    {
        // Arrange
        var reader = new HttpApiReader<int>(ct =>
        {
            var channel = Channel.CreateUnbounded<int>();
            channel.Writer.TryWrite(4);
            channel.Writer.TryWrite(5);
            channel.Writer.Complete();
            return channel.Reader.ReadAllAsync(ct);
        });

        var results = new List<int>();

        // Act
        await foreach (var item in reader.ReadAsync(TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().BeEquivalentTo([4, 5], options => options.WithStrictOrdering());
    }

    [Fact]
    public void Constructor_WithNull_ShouldThrow()
    {
        // Assert
        Action act1 = () => _ = new HttpApiReader<int>((Func<CancellationToken, Task<IEnumerable<int>>>)null!);
        act1.Should().Throw<ArgumentNullException>();

        Action act2 = () => _ = new HttpApiReader<int>((Func<CancellationToken, IAsyncEnumerable<int>>)null!);
        act2.Should().Throw<ArgumentNullException>();
    }
}
