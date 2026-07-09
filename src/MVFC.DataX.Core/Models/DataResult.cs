namespace MVFC.DataX.Core.Models;

public sealed record DataResult<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public IReadOnlyList<DataError> Errors { get; }

    internal DataResult(bool isSuccess, T? value, IReadOnlyList<DataError> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors;
    }
}

public static class DataResult
{
    public static DataResult<T> Success<T>(T value) => new(true, value, []);

    public static DataResult<T> Failure<T>(IEnumerable<DataError> errors) => new(false, default, [.. errors]);
}
