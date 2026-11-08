namespace MVFC.DataX.Core.Engine;

public interface IPipelineMiddleware<T>
{
    public IAsyncEnumerable<T> InvokeAsync(IAsyncEnumerable<T> source, CancellationToken ct = default);
}
