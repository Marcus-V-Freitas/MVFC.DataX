namespace MVFC.DataX.Tests.Transformers;

public sealed class TakeTransformerTests
{
    [Fact]
    public async Task TakeTransformer_Deve_Retornar_Ate_N_Elementos()
    {
        // Arrange
        var input = CreateAsyncEnumerable(1, 2, 3, 4, 5);
        var transformer = new TakeTransformer<int>(2);
        var results = new List<DataResult<int>>();

        // Act
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(2);
        results[0].Value.Should().Be(1);
        results[1].Value.Should().Be(2);
    }

    [Fact]
    public async Task TakeTransformer_Zero_Retorna_Vazio()
    {
        // Arrange
        var input = CreateAsyncEnumerable(1, 2, 3);
        var transformer = new TakeTransformer<int>(0);
        var results = new List<DataResult<int>>();

        // Act
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task TakeTransformer_Maior_Que_Tamanho_Retorna_Todos()
    {
        // Arrange
        var input = CreateAsyncEnumerable(1, 2);
        var transformer = new TakeTransformer<int>(5);
        var results = new List<DataResult<int>>();

        // Act
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(2);
    }
}
