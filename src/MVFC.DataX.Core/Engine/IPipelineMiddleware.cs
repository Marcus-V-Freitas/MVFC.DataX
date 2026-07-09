namespace MVFC.DataX.Core.Engine;

public interface IPipelineMiddleware<T>
{
    IAsyncEnumerable<T> InvokeAsync(IAsyncEnumerable<T> source, CancellationToken ct = default);
}
