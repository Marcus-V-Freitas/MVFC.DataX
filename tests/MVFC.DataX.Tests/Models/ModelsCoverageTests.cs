namespace MVFC.DataX.Tests.Models;

public sealed class ModelsCoverageTests
{
    [Fact]
    public void DataResult_Skipped_DeveRetornarInstanciaCorreta()
    {
        // Act
        var result = DataResult.Skipped<int>();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeFalse();
        result.IsSkipped.Should().BeTrue();
    }

    [Fact]
    public void DataResult_Failure_ComMensagem_DeveCriarErrorCorretamente()
    {
        // Act
        var result = DataResult.Failure<int>([new DataError("Minha prop", "Meu erro", 123)]);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        var err = result.Errors[0];
        err.PropertyName.Should().Be("Minha prop");
        err.ErrorMessage.Should().Be("Meu erro");
        err.AttemptedValue.Should().Be(123);
    }

    [Fact]
    public void DataResult_Failure_ComMensagemEMultiplusErros_DeveManterErros()
    {
        // Act
        var result = DataResult.Failure<int>([
            new DataError("", "Erro1", null), 
            new DataError("", "Erro2", null)
        ]);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Erro1");
        result.Errors.Select(e => e.ErrorMessage).Should().Contain("Erro2");
    }

    [Fact]
    public void PipelineStatistics_PropriedadesComputadas_DevemSerCorretas()
    {
        // Arrange
        var stats = new PipelineStatistics(
            TotalRead: 100,
            Succeeded: 80,
            Failed: 10,
            Skipped: 10,
            Elapsed: TimeSpan.FromSeconds(2),
            Errors: Array.Empty<DataError>()
        );

        // Act & Assert
        stats.Throughput.Should().Be(50); // 100 / 2
        stats.SuccessRate.Should().Be(80); // 80 / 100 * 100
        stats.FailureRate.Should().Be(10); // 10 / 100 * 100
    }

    [Fact]
    public void PipelineStatistics_ComTotalReadZero_NaoDeveDarErroDeDivisao()
    {
        // Arrange
        var stats = new PipelineStatistics(
            TotalRead: 0,
            Succeeded: 0,
            Failed: 0,
            Skipped: 0,
            Elapsed: TimeSpan.FromSeconds(0),
            Errors: Array.Empty<DataError>()
        );

        // Act & Assert
        stats.Throughput.Should().Be(0);
        stats.SuccessRate.Should().Be(0);
        stats.FailureRate.Should().Be(0);
    }
}
