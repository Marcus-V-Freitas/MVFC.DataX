namespace MVFC.DataX.Tests.Transformers;

public sealed class CompositeTransformerTests
{
    [Fact]
    public async Task CompositeTransformer_DevePropagar_ErrosDosPrimeirosEstagios()
    {
        // Arrange
        var failingFirst = new AlwaysFailTransformer<int, string>("STAGE_1_ERROR");
        var secondStage = Substitute.For<IDataTransformer<string, int>>();
        var composite = new CompositeTransformer<int, string, int>(failingFirst, secondStage);

        var input = CreateAsyncEnumerable(1, 2, 3);
        
        // Act
        var results = new List<DataResult<int>>();
        await foreach (var item in composite.TransformAsync(input, TestContext.Current.CancellationToken))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => 
        {
            r.IsFailure.Should().BeTrue();
            r.Errors.Should().Contain(e => e.ErrorMessage == "STAGE_1_ERROR");
        });

        secondStage
            .DidNotReceive()
            .TransformAsync(Arg.Any<IAsyncEnumerable<string>>(), Arg.Any<CancellationToken>());
    }

    private class AlwaysFailTransformer<TIn, TOut>(string errorMessage) : IDataTransformer<TIn, TOut>
    {
        public async IAsyncEnumerable<DataResult<TOut>> TransformAsync(IAsyncEnumerable<TIn> input, [EnumeratorCancellation] CancellationToken ct = default)
        {
            await foreach (var item in input.WithCancellation(ct))
            {
                yield return DataResult.Failure<TOut>([new DataError("Property", errorMessage, item)]);
            }
        }
    }
}
