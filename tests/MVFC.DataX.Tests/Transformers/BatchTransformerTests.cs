namespace MVFC.DataX.Tests.Transformers;

public sealed class BatchTransformerTests
{
    [Fact]
    public async Task BatchTransformer_Deve_Agrupar_Itens()
    {
        // Arrange
        var input = CreateAsyncEnumerable(1, 2, 3, 4, 5, 6, 7);
        var transformer = new BatchTransformer<int>(3);
        var results = new List<DataResult<IReadOnlyList<int>>>();

        // Act
        await foreach (var item in transformer.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(3);
        results[0].Value.Should().BeEquivalentTo([1, 2, 3]);
        results[1].Value.Should().BeEquivalentTo([4, 5, 6]);
        results[2].Value.Should().BeEquivalentTo([7]);
    }
}
