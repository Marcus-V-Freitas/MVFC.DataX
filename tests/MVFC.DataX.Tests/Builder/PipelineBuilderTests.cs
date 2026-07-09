namespace MVFC.DataX.Tests.Builder;

public sealed class PipelineBuilderTests
{
    [Fact]
    public void PipelineBuilder_Sem_Writer_Deve_Lancar_Excecao()
    {
        // Arrange
        var builder = PipelineBuilder
            .ReadFrom(new EnumerableReader<int>([1]))
            .TransformWith(new PassthroughTransformer<int>());

        // Act
        Action act = () => builder.Build();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Writer must be configured.");
    }

    [Fact]
    public void PipelineBuilder_Deve_Construir_Pipeline_Completo()
    {
        // Arrange
        var reader = new EnumerableReader<int>([1]);
        var writer = new InMemoryWriter<int>();
        var dlq = new InMemoryWriter<DataResult<int>>();

        // Act
        var pipeline = PipelineBuilder
            .ReadFrom(reader)
            .TransformWith(new PassthroughTransformer<int>())
            .WriteTo(writer)
            .OnError(ErrorHandling.DeadLetter(dlq))
            .WithParallelism(4)
            .WithBatchSize(50)
            .WithChannelCapacity(500)
            .WithRetry(3, TimeSpan.FromMilliseconds(10))
            .OnCompleted(stats => Task.CompletedTask)
            .Build();

        // Assert
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void PipelineBuilder_Deve_Suportar_Metodos_De_Extensao_Sem_Transformador_Inicial()
    {
        // Arrange
        var reader = new EnumerableReader<int>([1, 2, 3]);
        
        // Act
        var builder = PipelineBuilder
            .ReadFrom(reader)
            .Skip(1)
            .Take(2)
            .Distinct()
            .OrderBy(x => x)
            .FlatMap(x => new[] { x, x })
            .Aggregate(0, (a, b) => a + b)
            .Batch(10);
            
        // Assert
        builder.Should().NotBeNull();
    }

    [Fact]
    public void PipelineBuilder_Deve_Suportar_Metodos_De_Extensao_Com_Transformador()
    {
        // Arrange
        var reader = new EnumerableReader<int>([1, 2, 3]);
        var writer = new InMemoryWriter<IReadOnlyList<int>>();

        // Act
        var pipeline = PipelineBuilder
            .ReadFrom(reader)
            .TransformWith(new PassthroughTransformer<int>())
            .Skip(1)
            .Take(2)
            .Distinct()
            .OrderBy(x => x)
            .FlatMap(x => new[] { x, x })
            .Aggregate(0, (a, b) => a + b)
            .Batch(10)
            .WriteTo(writer)
            .Build();
            
        // Assert
        pipeline.Should().NotBeNull();
    }
}
