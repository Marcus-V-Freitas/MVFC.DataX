namespace MVFC.DataX.Pipeline;

public static class ErrorHandling
{
    public static IDataWriter<DataResult<T>> DeadLetter<T>(IDataWriter<DataResult<T>> writer) =>
        new DeadLetterWriter<T>(writer);

    public static IDataWriter<DataResult<T>> Ignore<T>() => 
        new IgnoreWriter<T>();

    public static IDataWriter<DataResult<T>> LogAndDiscard<T>(Action<DataResult<T>> logAction) => 
        new LogAndDiscardWriter<T>(logAction);
}

internal sealed class DeadLetterWriter<T>(IDataWriter<DataResult<T>> innerWriter) : IDataWriter<DataResult<T>>
{
    public Task WriteAsync(DataResult<T> item, CancellationToken ct = default) => innerWriter.WriteAsync(item, ct);
    public Task WriteBatchAsync(IReadOnlyList<DataResult<T>> batch, CancellationToken ct = default) => innerWriter.WriteBatchAsync(batch, ct);
}

internal sealed class IgnoreWriter<T> : IDataWriter<DataResult<T>>
{
    public Task WriteAsync(DataResult<T> item, CancellationToken ct = default) => Task.CompletedTask;
    public Task WriteBatchAsync(IReadOnlyList<DataResult<T>> batch, CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class LogAndDiscardWriter<T>(Action<DataResult<T>> logAction) : IDataWriter<DataResult<T>>
{
    public Task WriteAsync(DataResult<T> item, CancellationToken ct = default)
    {
        logAction(item);
        return Task.CompletedTask;
    }
    public Task WriteBatchAsync(IReadOnlyList<DataResult<T>> batch, CancellationToken ct = default)
    {
        foreach(var item in batch) logAction(item);
        return Task.CompletedTask;
    }
}
