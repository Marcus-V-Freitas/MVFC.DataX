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

    [Fact]
    public async Task OrderByTransformer_NaoDeveEstourarMemoria_Com1MilhaoDeItens()
    {
        // Arrange
        var input = GenerateMillions(TestContext.Current.CancellationToken);
        var transformer = new OrderByTransformer<int, int>(x => x, descending: true);

        GC.Collect();
        var before = GC.GetTotalMemory(true);

        // Act
        var results = new List<DataResult<int>>(1_000_000);
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        var after = GC.GetTotalMemory(true);

        // Assert
        results.Should().HaveCount(1_000_000);

        // Ensure memory used is reasonable (< 500MB is way more than enough for 1M integers)
        var usedBytes = after - before;
        usedBytes.Should().BeLessThan(500L * 1024 * 1024);
    }

    private static async IAsyncEnumerable<int> GenerateMillions([EnumeratorCancellation] CancellationToken ct = default)
    {
        for (var i = 0; i < 1_000_000; i++)
        {
            ct.ThrowIfCancellationRequested();
            yield return i;
        }
    }
}
