namespace MVFC.DataX.Tests.Readers;

public sealed class MergeReaderTests
{
    [Fact]
    public async Task MergeReader_DeveLerDeMultiplosLeitores_Sequencialmente()
    {
        // Arrange
        var r1 = new EnumerableReader<int>([1, 2]);
        var r2 = new EnumerableReader<int>([3, 4]);

        var mergeReader = new MergeReader<int>(r1, r2);
        var results = new List<int>();

        // Act
        await foreach (var item in mergeReader.ReadAsync(TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().BeEquivalentTo([1, 2, 3, 4], options => options.WithStrictOrdering());
    }

    [Fact]
    public void MergeReader_ComReadersNulos_DeveLancarArgumentNullException()
    {
        // Arrange
        IDataReader<int>[] nullReaders = null!;

        // Act
        Action act = () => _ = new MergeReader<int>(nullReaders);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task MergeReader_DevePropagarCancellationToken()
    {
        // Arrange
        var r1 = new EnumerableReader<int>([1]);
        var r2 = Substitute.For<IDataReader<int>>();

        r2.ReadAsync(Arg.Any<CancellationToken>())
            .Returns(CreateAsyncEnumerableWithCancellation<int>(TestContext.Current.CancellationToken));

        var mergeReader = new MergeReader<int>(r1, r2);

        // Act
        var act = async () =>
        {
            await foreach (var item in mergeReader.ReadAsync(TestContext.Current.CancellationToken))
            {
                // do nothing
            }
        };

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static async IAsyncEnumerable<T> CreateAsyncEnumerableWithCancellation<T>([EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.Yield();
        ThrowCancellation(ct);
        yield break;
    }

    private static void ThrowCancellation(CancellationToken ct) => throw new OperationCanceledException(ct);
}
