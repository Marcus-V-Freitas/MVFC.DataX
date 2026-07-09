namespace MVFC.DataX.Tests.Transformers;

public sealed class MapTransformerTests
{
    [Fact]
    public async Task MapTransformer_Deve_mapear_itens_com_sucesso()
    {
        // Arrange
        var transformer = new MapTransformer<int, string>(i => i.ToString(CultureInfo.InvariantCulture));
        var input = CreateAsyncEnumerable(1, 2, 3);
        
        // Act
        var results = await transformer.TransformAsync(input, TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        
        // Assert
        results.Should().HaveCount(3);
        results.All(r => r.IsSuccess).Should().BeTrue();
        results.Select(r => r.Value).Should().BeEquivalentTo("1", "2", "3");
    }

    [Fact]
    public async Task MapTransformer_Deve_pular_itens_nulos()
    {
        // Arrange
        var transformer = new MapTransformer<int, string>(i => i % 2 == 0 ? i.ToString(CultureInfo.InvariantCulture) : null);
        var input = CreateAsyncEnumerable(1, 2, 3, 4);
        
        // Act
        var results = await transformer.TransformAsync(input, TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        
        // Assert
        results.Should().HaveCount(2);
        results.Select(r => r.Value).Should().BeEquivalentTo("2", "4");
    }

    [Fact]
    public async Task MapTransformer_Deve_retornar_falha_em_excecao()
    {
        // Arrange
        var transformer = new MapTransformer<int, string>(i => i == 2 ? throw new InvalidOperationException("Erro") : i.ToString(CultureInfo.InvariantCulture));
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
