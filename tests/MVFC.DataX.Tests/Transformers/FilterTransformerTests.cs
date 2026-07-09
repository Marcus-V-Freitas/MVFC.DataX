namespace MVFC.DataX.Tests.Transformers;

public sealed class FilterTransformerTests
{
    [Fact]
    public async Task FilterTransformer_Deve_manter_itens_que_passam_no_filtro()
    {
        // Arrange
        var transformer = new FilterTransformer<int>(i => i % 2 == 0);
        var input = CreateAsyncEnumerable(1, 2, 3, 4);
        
        // Act
        var results = await transformer.TransformAsync(input, TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        
        // Assert
        results.Should().HaveCount(2);
        results.Select(r => r.Value).Should().BeEquivalentTo([2, 4]);
    }

    [Fact]
    public async Task FilterTransformer_Deve_lidar_com_excecoes_no_predicado()
    {
        // Arrange
        var transformer = new FilterTransformer<int>(i => i == 2 ? throw new InvalidOperationException("Erro") : true);
        var input = CreateAsyncEnumerable(1, 2, 3);
        
        // Act
        var results = await transformer.TransformAsync(input, TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        
        // Assert
        results.Should().HaveCount(3);
        results[0].IsSuccess.Should().BeTrue();
        
        results[1].IsFailure.Should().BeTrue();
        results[1].Errors[0].ErrorMessage.Should().Be("Erro");
        
        results[2].IsSuccess.Should().BeTrue();
    }
}
