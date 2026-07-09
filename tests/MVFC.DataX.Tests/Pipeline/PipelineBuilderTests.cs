namespace MVFC.DataX.Tests.Pipeline;

public sealed class PipelineBuilderTests
{
    [Fact]
    public void ReadFrom_ComIEnumerable_DeveCriarBuilder()
    {
        var source = new List<int> { 1, 2, 3 };
        var builder = PipelineBuilder.ReadFrom(source);

        builder.Should().NotBeNull();
    }

    [Fact]
    public void ReadFrom_ComIAsyncEnumerable_DeveCriarBuilder()
    {
        var source = CreateAsyncEnumerable(1, 2, 3);
        var builder = PipelineBuilder.ReadFrom(source);

        builder.Should().NotBeNull();
    }

    [Fact]
    public void ReadFrom_ComMultiplosReaders_DeveCriarBuilderComMergeReader()
    {
        var r1 = Substitute.For<IDataReader<int>>();
        var r2 = Substitute.For<IDataReader<int>>();

        var builder = PipelineBuilder.ReadFrom(r1, r2);

        builder.Should().NotBeNull();
    }

    [Fact]
    public void ReadFrom_ComReadersVazios_DeveLancarArgumentException()
    {
        Action act = () => PipelineBuilder.ReadFrom(Array.Empty<IDataReader<int>>());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void PipelineBuilder_ConfiguracoesOptions_DevemSerAtribuidasNaEngine()
    {
        // Arrange
        var reader = Substitute.For<IDataReader<int>>();
        var writer = Substitute.For<IDataWriter<string>>();
        var deadLetter = Substitute.For<IDataWriter<DataResult<string>>>();

        Func<PipelineStatistics, Task> onCompleted = (stats) => Task.CompletedTask;

        // Act
        var pipeline = PipelineBuilder.ReadFrom(reader)
            .TransformWith(new MapTransformer<int, string>(x => x.ToString(CultureInfo.InvariantCulture)))
            .WriteTo(writer)
            .WithParallelism(4)
            .WithBatchSize(100)
            .WithChannelCapacity(500)
            .WithRetry(3, TimeSpan.FromSeconds(2))
            .OnError(deadLetter)
            .OnCompleted(onCompleted)
            .Build();

        // Assert
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void Build_SemWriter_DeveLancarInvalidOperationException()
    {
        var reader = Substitute.For<IDataReader<int>>();

        var builder = PipelineBuilder.ReadFrom(reader)
            .TransformWith(new PassthroughTransformer<int>());

        Action act = () => builder.Build();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Writer must be configured.");
    }

    [Fact]
    public void PipelineBuilder_TInput_MetodosDeTransformacao_DevemRetornarBuilderValido()
    {
        var reader = Substitute.For<IDataReader<int>>();
        var builder = PipelineBuilder.ReadFrom(reader);

        builder.Skip(1).Should().NotBeNull();
        builder.Take(1).Should().NotBeNull();
        builder.Distinct().Should().NotBeNull();
        builder.OrderBy(x => x).Should().NotBeNull();
        builder.FlatMap(x => new[] { x }).Should().NotBeNull();
        builder.Aggregate(0, (acc, x) => acc + x).Should().NotBeNull();
        builder.Batch(10).Should().NotBeNull();
    }

    [Fact]
    public void PipelineBuilder_TInputTOutput_MetodosDeTransformacao_DevemRetornarBuilderValido()
    {
        var reader = Substitute.For<IDataReader<int>>();
        var builder = PipelineBuilder.ReadFrom(reader).TransformWith(new PassthroughTransformer<int>());

        builder.Skip(1).Should().NotBeNull();
        builder.Take(1).Should().NotBeNull();
        builder.Distinct().Should().NotBeNull();
        builder.OrderBy(x => x).Should().NotBeNull();
        builder.FlatMap(x => new[] { x }).Should().NotBeNull();
        builder.Aggregate(0, (acc, x) => acc + x).Should().NotBeNull();
        builder.Batch(10).Should().NotBeNull();
    }

    [Fact]
    public void ReplaceWriter_DeveSubstituirWriterSeExistir()
    {
        var reader = Substitute.For<IDataReader<int>>();
        var writer1 = Substitute.For<IDataWriter<int>>();
        var writer2 = Substitute.For<IDataWriter<int>>();

        var builder = PipelineBuilder.ReadFrom(reader)
            .TransformWith(new PassthroughTransformer<int>())
            .ReplaceWriter(w => writer2); // Não faz nada se não tiver writer ainda

        builder.WriteTo(writer1);
        builder.ReplaceWriter(w => writer2); // Agora sim substitui

        var pipeline = builder.Build();
        pipeline.Should().NotBeNull();
    }
}
