namespace MVFC.DataX.Tests.Writers;

public sealed class DelegateWriterTests
{
    [Fact]
    public async Task DelegateWriter_Deve_repassar_itens_ao_delegate()
    {
        // Arrange
        var items = new List<int>();
        var writer = new DelegateWriter<int>((batch, ct) =>
        {
            items.AddRange(batch);
            return Task.CompletedTask;
        });
        
        // Act
        await writer.WriteAsync(1, TestContext.Current.CancellationToken);
        await writer.WriteBatchAsync([2, 3], TestContext.Current.CancellationToken);
        
        // Assert
        items.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public async Task DelegateWriter_Deve_propagar_excecoes()
    {
        // Arrange
        var writer = new DelegateWriter<int>((batch, ct) => throw new InvalidOperationException("Erro"));
        
        // Act
        Func<Task> act = () => writer.WriteAsync(1, TestContext.Current.CancellationToken);
        
        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Erro");
    }
}
