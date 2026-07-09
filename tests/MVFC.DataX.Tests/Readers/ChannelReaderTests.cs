namespace MVFC.DataX.Tests.Readers;

public sealed class ChannelReaderTests
{
    [Fact]
    public async Task ChannelReader_ShouldReadFromChannel()
    {
        // Arrange
        var channel = System.Threading.Channels.Channel.CreateUnbounded<int>();
        var reader = new ChannelDataReader<int>(channel.Reader);

        _ = Task.Run(async () =>
        {
            await channel.Writer.WriteAsync(1, TestContext.Current.CancellationToken);
            await channel.Writer.WriteAsync(2, TestContext.Current.CancellationToken);
            await channel.Writer.WriteAsync(3, TestContext.Current.CancellationToken);
            channel.Writer.Complete();
        }, TestContext.Current.CancellationToken);

        // Act
        var results = await reader.ReadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        results.Should().BeEquivalentTo([1, 2, 3]);
    }
}
