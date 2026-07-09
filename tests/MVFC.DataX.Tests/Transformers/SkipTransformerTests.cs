namespace MVFC.DataX.Tests.Transformers;

public sealed class SkipTransformerTests
{
    [Fact]
    public async Task SkipTransformer_Deve_Ignorar_N_Elementos()
    {
        // Arrange
        var input = CreateAsyncEnumerable(1, 2, 3, 4, 5);
        var transformer = new SkipTransformer<int>(3);
        var results = new List<DataResult<int>>();

        // Act
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(2);
        results[0].Value.Should().Be(4);
        results[1].Value.Should().Be(5);
    }

    [Fact]
    public async Task SkipTransformer_Com_Zero_Nao_Ignora_Nada()
    {
        // Arrange
        var input = CreateAsyncEnumerable(1, 2, 3);
        var transformer = new SkipTransformer<int>(0);
        var results = new List<DataResult<int>>();

        // Act
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(3);
        results.Select(r => r.Value).Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public async Task SkipTransformer_Maior_Que_Elementos_Retorna_Vazio()
    {
        // Arrange
        var input = CreateAsyncEnumerable(1, 2);
        var transformer = new SkipTransformer<int>(5);
        var results = new List<DataResult<int>>();

        // Act
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().BeEmpty();
    }
}
