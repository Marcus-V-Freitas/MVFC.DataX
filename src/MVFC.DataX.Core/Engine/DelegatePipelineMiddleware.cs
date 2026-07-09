namespace MVFC.DataX.Core.Engine;

public sealed class DelegatePipelineMiddleware<T>(Func<IAsyncEnumerable<T>, CancellationToken, IAsyncEnumerable<T>> middlewareFunc) : IPipelineMiddleware<T>
{
    private readonly Func<IAsyncEnumerable<T>, CancellationToken, IAsyncEnumerable<T>> _middlewareFunc = middlewareFunc ?? throw new ArgumentNullException(nameof(middlewareFunc));

    public IAsyncEnumerable<T> InvokeAsync(IAsyncEnumerable<T> source, CancellationToken ct = default)
        => _middlewareFunc(source, ct);
}
