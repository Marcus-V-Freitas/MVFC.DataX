namespace MVFC.DataX.Tests.Pipeline;

public sealed class ErrorHandlingWritersTests
{
    [Fact]
    public async Task IgnoreWriter_DeveApenasRetornarTaskCompleted()
    {
        // Arrange
        var writer = ErrorHandling.Ignore<int>();
        var item = DataResult.Success(10);
        var batch = new List<DataResult<int>> { item };

        // Act & Assert (Should not throw)
        await writer.WriteAsync(item, TestContext.Current.CancellationToken);
        await writer.WriteBatchAsync(batch, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task LogAndDiscardWriter_DeveInvocarAction()
    {
        // Arrange
        var invokeCount = 0;
        void Action(DataResult<int> r) => invokeCount++;

        var writer = ErrorHandling.LogAndDiscard((Action<DataResult<int>>)Action);
        var item = DataResult.Success(10);
        var batch = new List<DataResult<int>> { item, item };

        // Act
        await writer.WriteAsync(item, TestContext.Current.CancellationToken);
        await writer.WriteBatchAsync(batch, TestContext.Current.CancellationToken);

        // Assert
        invokeCount.Should().Be(3);
    }

    [Fact]
    public async Task DeadLetterWriter_DeveRepassarChamadaParaInnerWriter()
    {
        // Arrange
        var innerWriter = Substitute.For<IDataWriter<DataResult<int>>>();
        var writer = ErrorHandling.DeadLetter(innerWriter);
        var item = DataResult.Success(10);
        var batch = new List<DataResult<int>> { item };

        // Act
        await writer.WriteAsync(item, TestContext.Current.CancellationToken);
        await writer.WriteBatchAsync(batch, TestContext.Current.CancellationToken);

        // Assert
        await innerWriter.Received(1).WriteAsync(item, Arg.Any<CancellationToken>());
        await innerWriter.Received(1).WriteBatchAsync(batch, Arg.Any<CancellationToken>());
    }
}
