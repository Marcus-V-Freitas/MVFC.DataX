namespace MVFC.DataX.Tests.Transformers;

public sealed class FlatMapTransformerTests
{
    [Fact]
    public async Task FlatMapTransformer_Deve_Explodir_Listas()
    {
        // Arrange
        var input = CreateAsyncEnumerable<int[]>(
            [1, 2],
            [3, 4, 5]
        );
        var transformer = new FlatMapTransformer<int[], int>(x => x);
        var results = new List<DataResult<int>>();

        // Act
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Select(r => r.Value).Should().BeEquivalentTo([1, 2, 3, 4, 5], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task FlatMapTransformer_Retorna_Falha_Para_Nulos()
    {
        // Arrange
        var input = CreateAsyncEnumerable<int[]?>(
            [1],
            null,
            [2]
        );
        var transformer = new FlatMapTransformer<int[]?, int>(x => x);
        var results = new List<DataResult<int>>();

        // Act
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(3);
        results.Count(r => r.IsSuccess).Should().Be(2);
        results.Count(r => r.IsFailure).Should().Be(1);
        results.Where(r => r.IsSuccess).Select(r => r.Value).Should().BeEquivalentTo([1, 2], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task FlatMapTransformer_Deve_Capturar_Excecao_Na_Funcao()
    {
        // Arrange
        var input = CreateAsyncEnumerable(1, 2, 3);
        var transformer = new FlatMapTransformer<int, int>(x => x == 2 ? throw new InvalidOperationException("Erro FlatMap") : [x * 10]);
        var results = new List<DataResult<int>>();

        // Act
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(3); // 1 success, 1 fail, 1 success
        results[0].IsSuccess.Should().BeTrue();
        results[0].Value.Should().Be(10);

        results[1].IsFailure.Should().BeTrue();
        results[1].Errors[0].ErrorMessage.Should().Be("Erro FlatMap");

        results[2].IsSuccess.Should().BeTrue();
        results[2].Value.Should().Be(30);
    }
}
