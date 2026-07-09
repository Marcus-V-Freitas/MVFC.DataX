namespace MVFC.DataX.Tests.Writers;

public sealed class InMemoryWriterTests
{
    [Fact]
    public async Task InMemoryWriter_Deve_acumular_itens_escritos()
    {
        // Arrange
        var writer = new InMemoryWriter<int>();
        
        // Act
        await writer.WriteAsync(1, TestContext.Current.CancellationToken);
        await writer.WriteBatchAsync([2, 3], TestContext.Current.CancellationToken);
        
        // Assert
        writer.Items.Should().BeEquivalentTo([1, 2, 3]);
    }
}
