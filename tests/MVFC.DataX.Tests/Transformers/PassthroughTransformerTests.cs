namespace MVFC.DataX.Tests.Transformers;

public sealed class PassthroughTransformerTests
{
    [Fact]
    public async Task PassthroughTransformer_Deve_repassar_itens()
    {
        // Arrange
        var transformer = new PassthroughTransformer<int>();
        var input = CreateAsyncEnumerable(1, 2, 3);
        
        // Act
        var results = await transformer.TransformAsync(input, TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        
        // Assert
        results.Should().HaveCount(3);
        results.All(r => r.IsSuccess).Should().BeTrue();
        results.Select(r => r.Value).Should().BeEquivalentTo([1, 2, 3]);
    }
}
