namespace MVFC.DataX.Tests;

public sealed class CoreTests
{
    [Fact]
    public void DataResult_Deve_criar_sucesso_com_valor()
    {
        // Arrange & Act
        var result = DataResult.Success("test");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be("test");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void DataResult_Deve_criar_falha_com_erros()
    {
        // Arrange
        var errors = new[] { new DataError("Prop", "Err", null) };

        // Act
        var result = DataResult.Failure<string>(errors);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Value.Should().BeNull();
        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("Err");
    }

    [Fact]
    public async Task PipelineEngine_Deve_processar_itens_com_backpressure()
    {
        // Arrange
        var reader = Substitute.For<IDataReader<int>>();
        reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(CreateAsyncEnumerable(1, 2, 3));

        var transformer = Substitute.For<IDataTransformer<int, string>>();
        transformer.TransformAsync(Arg.Any<IAsyncEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable(DataResult.Success("1"), DataResult.Success("2"), DataResult.Success("3")));

        var writer = Substitute.For<IDataWriter<string>>();

        var engine = new PipelineEngine<int, string>(reader, transformer, writer, new PipelineOptions { Parallelism = 1, BatchSize = 2 });

        // Act
        var stats = await engine.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        stats.Succeeded.Should().Be(3);
        stats.Failed.Should().Be(0);
        await writer.Received(1).WriteBatchAsync(Arg.Is<IReadOnlyList<string>>(b => b.Count == 2), Arg.Any<CancellationToken>());
        await writer.Received(1).WriteBatchAsync(Arg.Is<IReadOnlyList<string>>(b => b.Count == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PipelineEngine_Deve_encaminhar_falhas_para_dead_letter()
    {
        // Arrange
        var reader = Substitute.For<IDataReader<int>>();
        reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(CreateAsyncEnumerable(1, 2));

        var transformer = Substitute.For<IDataTransformer<int, string>>();
        transformer.TransformAsync(Arg.Any<IAsyncEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable(DataResult.Failure<string>([new DataError("Prop", "Error", null)]), DataResult.Success("2")));

        var writer = Substitute.For<IDataWriter<string>>();
        var dlqWriter = Substitute.For<IDataWriter<DataResult<string>>>();

        var engine = new PipelineEngine<int, string>(reader, transformer, writer, new PipelineOptions(), deadLetterWriter: dlqWriter);

        // Act
        var stats = await engine.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        stats.Succeeded.Should().Be(1);
        stats.Failed.Should().Be(1);
        stats.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("Error");

        await dlqWriter.Received(1).WriteAsync(Arg.Is<DataResult<string>>(r => r.IsFailure), Arg.Any<CancellationToken>());
        await writer.Received(1).WriteBatchAsync(Arg.Is<IReadOnlyList<string>>(b => b.Count == 1), Arg.Any<CancellationToken>());
    }

}
