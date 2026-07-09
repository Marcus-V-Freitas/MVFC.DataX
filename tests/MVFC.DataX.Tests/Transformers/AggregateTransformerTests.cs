namespace MVFC.DataX.Tests.Transformers;

public sealed class AggregateTransformerTests
{
    [Fact]
    public async Task AggregateTransformer_Deve_Somar_Valores()
    {
        // Arrange
        var input = CreateAsyncEnumerable(1, 2, 3, 4);
        var transformer = new AggregateTransformer<int, int>(0, (acc, val) => acc + val);
        var results = new List<DataResult<int>>();

        // Act
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().ContainSingle();
        results[0].Value.Should().Be(10);
    }

    [Fact]
    public async Task AggregateTransformer_Deve_Capturar_Excecao_Na_Agregacao()
    {
        // Arrange
        var input = CreateAsyncEnumerable(1, 2, 3);
        var transformer = new AggregateTransformer<int, int>(0, (acc, val) => val == 2 ? throw new InvalidOperationException("Erro de Teste") : acc + val);
        var results = new List<DataResult<int>>();

        // Act
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().ContainSingle();
        results[0].IsFailure.Should().BeTrue();
        results[0].Errors[0].ErrorMessage.Should().Be("Erro de Teste");
    }
}
