namespace MVFC.DataX.Tests.Readers;

public sealed class EnumerableReaderTests
{
    [Fact]
    public async Task EnumerableReader_Deve_ler_de_IEnumerable()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };
        var reader = new EnumerableReader<int>(list);
        
        // Act
        var results = await reader.ReadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        
        // Assert
        results.Should().BeEquivalentTo(list);
    }

    [Fact]
    public async Task EnumerableReader_Deve_ler_de_IAsyncEnumerable()
    {
        // Arrange
        var input = CreateAsyncEnumerable(1, 2, 3);
        var reader = new EnumerableReader<int>(input);
        
        // Act
        var results = await reader.ReadAsync(TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);
        
        // Assert
        results.Should().BeEquivalentTo([1, 2, 3]);
    }
}
