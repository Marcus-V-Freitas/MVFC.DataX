namespace MVFC.DataX.Pipeline;

public static class ErrorHandling
{
    public static IDataWriter<DataResult<T>> DeadLetter<T>(IDataWriter<DataResult<T>> writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        return new DeadLetterWriter<T>(writer);
    }

    public static IDataWriter<DataResult<T>> Ignore<T>() =>
        new IgnoreWriter<T>();

    public static IDataWriter<DataResult<T>> LogAndDiscard<T>(Action<DataResult<T>> logAction)
    {
        ArgumentNullException.ThrowIfNull(logAction);
        return new LogAndDiscardWriter<T>(logAction);
    }
}
