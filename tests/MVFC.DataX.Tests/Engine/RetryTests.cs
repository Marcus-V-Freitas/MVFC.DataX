namespace MVFC.DataX.Tests.Engine;

public sealed class RetryTests
{
    [Fact]
    public async Task PipelineEngine_Deve_tentar_novamente_em_falha_de_escrita()
    {
        // Arrange
        var reader = new EnumerableReader<int>([1]);
        var transformer = new PassthroughTransformer<int>();

        var tryCount = 0;
        var writer = new DelegateWriter<int>((batch, ct) =>
        {
            tryCount++;
            return tryCount < 3 ? throw new InvalidOperationException("Temporary Error") : Task.CompletedTask;
        });

        var engine = new PipelineEngine<int, int>(
            reader, transformer, writer,
            options: new PipelineOptions { MaxRetries = 3, RetryDelay = TimeSpan.FromMilliseconds(1) });

        // Act
        var stats = await engine.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        stats.Succeeded.Should().Be(1);
        stats.Failed.Should().Be(0);
        stats.Skipped.Should().Be(0);
        stats.Elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
        stats.Errors.Should().BeEmpty();
        tryCount.Should().Be(3);
    }

    [Fact]
    public async Task PipelineEngine_Deve_falhar_se_estourar_retries()
    {
        // Arrange
        var reader = new EnumerableReader<int>([1]);
        var transformer = new PassthroughTransformer<int>();

        var tryCount = 0;
        var writer = new DelegateWriter<int>((batch, ct) =>
        {
            tryCount++;
            throw new InvalidOperationException("Permanent Error");
        });

        var engine = new PipelineEngine<int, int>(
            reader, transformer, writer,
            options: new PipelineOptions { MaxRetries = 2, RetryDelay = TimeSpan.Zero });

        // Act
        Func<Task> act = () => engine.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Permanent Error");
        tryCount.Should().Be(3);
    }
}
