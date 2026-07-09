namespace MVFC.DataX.Tests.Engine;

public sealed class PipelineCancellationTests
{
    [Fact]
    public async Task Pipeline_DeveLancar_OperationCanceledException_QuandoTokenCancelado()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        var reader = new InfiniteReader();
        var writer = Substitute.For<IDataWriter<int>>();

        var pipeline = PipelineBuilder
            .ReadFrom(reader)
            .TransformWith(new PassthroughTransformer<int>())
            .WriteTo(writer)
            .Build();

        // Act
        var act = () => pipeline.RunAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private class InfiniteReader : IDataReader<int>
    {
        public async IAsyncEnumerable<int> ReadAsync([EnumeratorCancellation] CancellationToken ct = default)
        {
            int i = 0;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                yield return i++;
                await Task.Delay(10, ct); // Avoid tight loop
            }
        }
    }
}
