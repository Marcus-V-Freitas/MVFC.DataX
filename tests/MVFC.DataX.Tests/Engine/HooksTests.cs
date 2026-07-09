namespace MVFC.DataX.Tests.Engine;

public sealed class HooksTests
{
    [Fact]
    public async Task PipelineEngine_Deve_chamar_OnCompleted_com_estatisticas_corretas()
    {
        // Arrange
        var reader = new EnumerableReader<int>([1, 2, 3, 4]);
        var transformer = new MapTransformer<int, string>(i => i % 2 == 0 ? i.ToString(CultureInfo.InvariantCulture) : null);
        var writer = new InMemoryWriter<string>();

        PipelineStatistics? finalStats = null;

        var engine = new PipelineEngine<int, string>(
            reader, transformer, writer,
            new PipelineOptions(),
            onCompleted: stats =>
            {
                finalStats = stats;
                return Task.CompletedTask;
            });

        // Act
        await engine.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        finalStats.Should().NotBeNull();
        finalStats!.TotalRead.Should().Be(4);
        finalStats.Succeeded.Should().Be(2);
        finalStats.Failed.Should().Be(2);
        finalStats.Skipped.Should().Be(0);
    }
}
