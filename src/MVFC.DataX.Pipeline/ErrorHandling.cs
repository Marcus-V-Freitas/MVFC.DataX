namespace MVFC.DataX.Pipeline;

public static class ErrorHandling
{
    public static IDataWriter<DataResult<T>> DeadLetter<T>(IDataWriter<DataResult<T>> writer) =>
        writer;
}
