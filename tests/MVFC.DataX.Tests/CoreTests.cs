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

        var capturedBatches = new List<List<string>>();
        var writer = Substitute.For<IDataWriter<string>>();
        writer.WriteBatchAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedBatches.Add([.. (IReadOnlyList<string>)call[0]]);
                return Task.CompletedTask;
            });

        var engine = new PipelineEngine<int, string>(reader, transformer, writer, new PipelineOptions { Parallelism = 1, BatchSize = 2 });

        // Act
        var stats = await engine.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        stats.Succeeded.Should().Be(3);
        stats.Failed.Should().Be(0);
        capturedBatches.Should().HaveCount(2);
        capturedBatches[0].Should().HaveCount(2);
        capturedBatches[1].Should().HaveCount(1);
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

        var capturedBatches = new List<List<string>>();
        var writer = Substitute.For<IDataWriter<string>>();
        writer.WriteBatchAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                capturedBatches.Add([.. (IReadOnlyList<string>)call[0]]);
                return Task.CompletedTask;
            });

        var dlqWriter = Substitute.For<IDataWriter<DataResult<string>>>();

        var engine = new PipelineEngine<int, string>(reader, transformer, writer, new PipelineOptions(), deadLetterWriter: dlqWriter);

        // Act
        var stats = await engine.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        stats.Succeeded.Should().Be(1);
        stats.Failed.Should().Be(1);
        stats.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be("Error");

        await dlqWriter.Received(1).WriteAsync(Arg.Is<DataResult<string>>(r => r.IsFailure), Arg.Any<CancellationToken>());
        capturedBatches.Should().ContainSingle().Which.Should().HaveCount(1);
    }

    [Fact]
    public void DataError_FromException_Deve_preservar_tipo_e_stacktrace()
    {
        // Arrange
        var exception = new InvalidOperationException("Test message");

        // Act
        var error = DataError.FromException(exception, attemptedValue: 123);

        // Assert
        error.PropertyName.Should().Be("Exception");
        error.ErrorMessage.Should().Be("Test message");
        error.AttemptedValue.Should().Be(123);
        error.ExceptionType.Should().Be(typeof(InvalidOperationException).FullName);
    }

    [Fact]
    public async Task PipelineEngine_Deve_executar_middlewares()
    {
        // Arrange
        var reader = Substitute.For<IDataReader<int>>();
        reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(CreateAsyncEnumerable(1, 2));

        var transformer = Substitute.For<IDataTransformer<int, int>>();
        async IAsyncEnumerable<DataResult<int>> TransformLocal(IAsyncEnumerable<int> input)
        {
            await foreach (var item in input)
            {
                yield return DataResult.Success(item);
            }
        }
        transformer.TransformAsync(Arg.Any<IAsyncEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(call => TransformLocal((IAsyncEnumerable<int>)call[0]));

        var writer = Substitute.For<IDataWriter<int>>();

        var middleware = Substitute.For<IPipelineMiddleware<int>>();
        async IAsyncEnumerable<int> MiddlewareLocal(IAsyncEnumerable<int> source)
        {
            await foreach (var item in source)
            {
                yield return item * 10;
            }
        }
        middleware.InvokeAsync(Arg.Any<IAsyncEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(call => MiddlewareLocal((IAsyncEnumerable<int>)call[0]));

        var engine = new PipelineEngine<int, int>(
            reader,
            transformer,
            writer,
            new PipelineOptions { BatchSize = 10 },
            middlewares: [middleware]);

        // Act
        var stats = await engine.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        stats.Succeeded.Should().Be(2);
        await writer.Received(1).WriteBatchAsync(Arg.Is<IReadOnlyList<int>>(b => b.SequenceEqual(new int[] { 10, 20 })), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DistinctTransformer_Com_MaxCapacity_Deve_remover_duplicados_na_janela()
    {
        // Arrange
        var transformer = new DistinctTransformer<int>(maxCapacity: 2);
        var input = CreateAsyncEnumerable(1, 2, 1, 3, 2);

        // Act
        var results = await transformer.TransformAsync(input, TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        var values = results.Select(r => r.Value);
        values.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task OrderByTransformer_Deve_lançar_exceção_se_exceder_maxItems()
    {
        // Arrange
        var transformer = new OrderByTransformer<int, int>(x => x, maxItems: 2);
        var input = CreateAsyncEnumerable(1, 2, 3);

        // Act & Assert
        var act = async () => await transformer.TransformAsync(input, TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PipelineEngine_Deve_usar_backoff_exponencial_e_chamar_telemetria()
    {
        // Arrange
        var reader = Substitute.For<IDataReader<int>>();
        reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(CreateAsyncEnumerable(1));

        var transformer = Substitute.For<IDataTransformer<int, int>>();
        transformer.TransformAsync(Arg.Any<IAsyncEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable(DataResult.Success(1)));

        var writer = Substitute.For<IDataWriter<int>>();
        var attempts = 0;
        writer.WriteBatchAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                attempts++;
                return attempts < 3 ? throw new Exception("Temporary error") : Task.CompletedTask;
            });

        var retriesList = new List<(Exception Ex, int Attempt, TimeSpan Delay)>();
        var options = new PipelineOptions
        {
            MaxRetries = 3,
            RetryDelay = TimeSpan.FromMilliseconds(5),
            UseExponentialBackoff = true,
            UseJitter = false,
            OnRetry = (ex, att, delay) => retriesList.Add((ex, att, delay))
        };

        var engine = new PipelineEngine<int, int>(reader, transformer, writer, options);

        // Act
        var stats = await engine.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        stats.Succeeded.Should().Be(1);
        attempts.Should().Be(3);
        retriesList.Should().HaveCount(2);
        retriesList[0].Attempt.Should().Be(1);
        retriesList[0].Delay.Should().Be(TimeSpan.FromMilliseconds(5));
        retriesList[1].Attempt.Should().Be(2);
        retriesList[1].Delay.Should().Be(TimeSpan.FromMilliseconds(10)); // 5 * 2^1
    }

    [Fact]
    public async Task PipelineEngine_Deve_respeitar_classificador_de_erros()
    {
        // Arrange
        var reader = Substitute.For<IDataReader<int>>();
        reader.ReadAsync(Arg.Any<CancellationToken>()).Returns(CreateAsyncEnumerable(1, 2));

        var transformer = Substitute.For<IDataTransformer<int, int>>();
        transformer.TransformAsync(Arg.Any<IAsyncEnumerable<int>>(), Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerable(DataResult.Success(1), DataResult.Success(2)));

        var writer = Substitute.For<IDataWriter<int>>();
        writer.WriteBatchAsync(Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var list = (IReadOnlyList<int>)call[0];
                if (list.Contains(1))
                {
                    throw new InvalidOperationException("Skip me");
                }

                return list.Contains(2) ? throw new ArgumentException("Deadletter me") : Task.CompletedTask;
            });

        var dlqWriter = Substitute.For<IDataWriter<DataResult<int>>>();
        var options = new PipelineOptions
        {
            BatchSize = 1,
            ErrorClassifier = ex => ex switch
            {
                InvalidOperationException => ErrorAction.Skip,
                ArgumentException => ErrorAction.DeadLetter,
                _ => ErrorAction.Abort
            }
        };

        var engine = new PipelineEngine<int, int>(reader, transformer, writer, options, deadLetterWriter: dlqWriter);

        // Act
        var stats = await engine.RunAsync(TestContext.Current.CancellationToken);

        // Assert
        stats.Succeeded.Should().Be(0);
        stats.Skipped.Should().Be(1);
        stats.Failed.Should().Be(1);
        await dlqWriter.Received(1).WriteAsync(Arg.Is<DataResult<int>>(r => r.IsFailure && r.Errors.Any(e => e.ExceptionType == typeof(ArgumentException).FullName)), Arg.Any<CancellationToken>());
    }
}
