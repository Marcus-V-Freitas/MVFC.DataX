namespace MVFC.DataX.Tests.Transformers;

public sealed class OrderByTransformerTests
{
    [Fact]
    public async Task OrderByTransformer_Deve_Ordenar_Ascendente()
    {
        // Arrange
        var input = CreateAsyncEnumerable(3, 1, 4, 2, 5);
        var transformer = new OrderByTransformer<int, int>(x => x);
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
    public async Task OrderByTransformer_Deve_Lidar_Com_Lista_Vazia()
    {
        // Arrange
        var input = CreateAsyncEnumerable<int>();
        var transformer = new OrderByTransformer<int, int>(x => x);
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
    public async Task OrderByTransformer_Deve_Ordenar_Descendente()
    {
        // Arrange
        var input = CreateAsyncEnumerable(3, 1, 4, 2, 5);
        var transformer = new OrderByTransformer<int, int>(x => x, descending: true);
        var results = new List<DataResult<int>>();

        // Act
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Select(r => r.Value).Should().BeEquivalentTo([5, 4, 3, 2, 1], options => options.WithStrictOrdering());
    }
}
