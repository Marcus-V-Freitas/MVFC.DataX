namespace MVFC.DataX.Tests.Transformers;

public sealed class DistinctTransformerTests
{
    [Fact]
    public async Task DistinctTransformer_Deve_Remover_Duplicatas()
    {
        // Arrange
        var input = CreateAsyncEnumerable(1, 2, 2, 3, 1, 4, 5, 5);
        var transformer = new DistinctTransformer<int>();
        var results = new List<DataResult<int>>();

        // Act
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Select(r => r.Value).Should().BeEquivalentTo([1, 2, 3, 4, 5]);
    }

    [Fact]
    public async Task DistinctTransformer_Respeita_Comparer()
    {
        // Arrange
        var input = CreateAsyncEnumerable("a", "A", "b", "C", "c");
        var transformer = new DistinctTransformer<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<DataResult<string>>();

        // Act
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Select(r => r.Value).Should().BeEquivalentTo(["a", "b", "C"]);
    }
}
